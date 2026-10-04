using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Exceptions;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Services;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

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
        var isTourImage = MediaPurposes.IsTourImage(request.Purpose);
        if (isTourImage)
            EnsureManagerOrAdmin();

        var fileId = Guid.NewGuid();
        var sanitizedFileName = FileNameSanitizer.Sanitize(request.FileName);
        var storageKey = $"{currentUser.UserId:N}/{fileId}/{sanitizedFileName}";

        int? width = null;
        int? height = null;
        if (isTourImage)
        {
            var dimensions = await ReadDimensionsAsync(request.FileContent, ct);
            width = dimensions.Width;
            height = dimensions.Height;
        }

        if (request.FileContent.CanSeek)
            request.FileContent.Position = 0;

        await storage.UploadAsync(request.FileContent, storageKey, request.ContentType, ct);

        var mediaFile = MediaFile.Create(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            storageKey,
            currentUser.UserId.ToString(),
            isTourImage ? MediaPurposes.TourImage : MediaPurposes.General,
            width,
            height);

        if (isTourImage)
            await AddTourPreviewsAsync(request, storageKey, mediaFile, ct);
        else if (imageProcessor.IsImage(request.ContentType))
            await AddLegacyThumbnailsAsync(request, storageKey, mediaFile, ct);

        await repository.AddAsync(mediaFile, ct);
        await repository.SaveChangesAsync(ct);

        if (isTourImage)
            return TourImageResponse(mediaFile, width, height);

        return await PresignedResponseAsync(mediaFile, ct);
    }

    private void EnsureManagerOrAdmin()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Authentication is required.");

        var role = currentUser.Role;
        if (!string.Equals(role, AppRoles.Manager, StringComparison.Ordinal)
            && !string.Equals(role, AppRoles.Admin, StringComparison.Ordinal))
        {
            throw new MediaAccessDeniedException("Tour image upload requires a manager or admin.");
        }
    }

    private async Task<ImageDimensions> ReadDimensionsAsync(Stream content, CancellationToken ct)
    {
        try
        {
            var dimensions = await imageProcessor.GetDimensionsAsync(content, ct);
            if (dimensions.Width <= 0 || dimensions.Height <= 0)
                throw new InvalidOperationException("Image dimensions are empty.");
            return dimensions;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["FileContent"] = ["File is not a valid image."]
            });
        }
    }

    private async Task AddTourPreviewsAsync(
        UploadMediaCommand request,
        string storageKey,
        MediaFile mediaFile,
        CancellationToken ct)
    {
        foreach (var (code, maxWidth) in TourImageSizes.All)
        {
            if (request.FileContent.CanSeek)
                request.FileContent.Position = 0;

            var resized = await imageProcessor.ResizeWithinAsync(request.FileContent, maxWidth, ct);
            await using (resized.Content)
            {
                var previewKey = $"{storageKey}-preview-{code}";
                await storage.UploadAsync(resized.Content, previewKey, resized.ContentType, ct);
                mediaFile.AddThumbnail(previewKey, resized.Width, resized.Height, code);
            }
        }
    }

    private async Task AddLegacyThumbnailsAsync(
        UploadMediaCommand request,
        string storageKey,
        MediaFile mediaFile,
        CancellationToken ct)
    {
        foreach (var thumbWidth in uploadOptions.Value.ThumbnailWidths)
        {
            request.FileContent.Position = 0;
            using var resized = await imageProcessor.ResizeAsync(request.FileContent, thumbWidth, ct);
            var thumbKey = $"{storageKey}-thumb-{thumbWidth}";
            await storage.UploadAsync(resized, thumbKey, request.ContentType, ct);
            mediaFile.AddThumbnail(thumbKey, thumbWidth, 0);
        }
    }

    private static UploadMediaResponse TourImageResponse(MediaFile mediaFile, int? width, int? height)
    {
        var thumbnailResponses = new List<ThumbnailResponse>(mediaFile.Thumbnails.Count);
        foreach (var thumb in mediaFile.Thumbnails)
        {
            var size = thumb.SizeCode ?? string.Empty;
            thumbnailResponses.Add(new ThumbnailResponse(
                CreateDeterministicGuid(thumb.StorageKey),
                thumb.Width,
                thumb.Height,
                TourImagePaths.Manage(mediaFile.Id, size)));
        }

        return new UploadMediaResponse(
            mediaFile.Id,
            TourImagePaths.Manage(mediaFile.Id, TourImageSizes.W1600),
            mediaFile.OriginalFileName,
            mediaFile.ContentType,
            mediaFile.SizeBytes,
            thumbnailResponses,
            mediaFile.UploadedAt,
            width,
            height,
            IsPublic: false);
    }

    private async Task<UploadMediaResponse> PresignedResponseAsync(MediaFile mediaFile, CancellationToken ct)
    {
        var ttl = TimeSpan.FromMinutes(storageOptions.Value.PresignTtlMinutes);
        var url = await storage.GeneratePresignedUrlAsync(mediaFile.StorageKey, ttl, ct);

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
