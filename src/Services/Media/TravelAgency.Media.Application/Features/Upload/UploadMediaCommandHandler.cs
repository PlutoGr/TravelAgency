using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Media.Application.Services;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Application.Features.Upload;

public sealed class UploadMediaCommandHandler(
    IStorageService storage,
    IImageProcessingService imageProcessor,
    IMediaFileRepository repository,
    ICurrentUserService currentUser,
    IOptions<StorageSettings> storageOptions,
    IOptions<UploadSettings> uploadOptions
) : IRequestHandler<UploadMediaCommand, UploadMediaResponse>
{
    public async Task<UploadMediaResponse> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var fileId = Guid.NewGuid();
        var sanitizedFileName = FileNameSanitizer.Sanitize(request.FileName);
        var storageKey = $"{currentUser.UserId:N}/{fileId}/{sanitizedFileName}";

        await storage.UploadAsync(request.FileContent, storageKey, request.ContentType, ct);

        var mediaFile = MediaFile.Create(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            storageKey,
            currentUser.UserId.ToString());

        if (imageProcessor.IsImage(request.ContentType))
        {
            foreach (var width in uploadOptions.Value.ThumbnailWidths)
            {
                request.FileContent.Position = 0;
                using var resized = await imageProcessor.ResizeAsync(request.FileContent, width, ct);
                var thumbKey = $"{storageKey}-thumb-{width}";
                await storage.UploadAsync(resized, thumbKey, request.ContentType, ct);
                mediaFile.AddThumbnail(thumbKey, width, 0);
            }
        }

        await repository.AddAsync(mediaFile, ct);
        await repository.SaveChangesAsync(ct);

        var ttl = TimeSpan.FromMinutes(storageOptions.Value.PresignTtlMinutes);
        var url = await storage.GeneratePresignedUrlAsync(storageKey, ttl, ct);

        var thumbnailResponses = new List<ThumbnailResponse>(mediaFile.Thumbnails.Count);
        foreach (var thumb in mediaFile.Thumbnails)
        {
            var thumbUrl = await storage.GeneratePresignedUrlAsync(thumb.StorageKey, ttl, ct);
            var thumbId = CreateDeterministicGuid(thumb.StorageKey);
            thumbnailResponses.Add(new ThumbnailResponse(thumbId, thumb.Width, thumb.Height, thumbUrl));
        }

        return new UploadMediaResponse(
            mediaFile.Id,
            url,
            mediaFile.OriginalFileName,
            mediaFile.ContentType,
            mediaFile.SizeBytes,
            thumbnailResponses,
            mediaFile.UploadedAt);
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return new Guid(hash.AsSpan(0, 16));
    }
}
