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
    IOptions<UploadSettings> uploadOptions
) : IRequestHandler<UploadMediaCommand, UploadMediaResponse>
{
    public async Task<UploadMediaResponse> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var isTourImage = MediaPurposes.IsTourImage(request.Purpose);
        var isAvatar = MediaPurposes.IsAvatar(request.Purpose);
        if (isTourImage)
            EnsureManagerOrAdmin();
        if (isAvatar)
            EnsureAuthenticated();

        var fileId = Guid.NewGuid();
        var sanitizedFileName = FileNameSanitizer.Sanitize(request.FileName);
        var storageKey = $"{currentUser.UserId:N}/{fileId}/{sanitizedFileName}";

        if (isAvatar)
            return await UploadAvatarAsync(request, fileId, storageKey, ct);

        int? width = null;
        int? height = null;
        var isImage = imageProcessor.IsImage(request.ContentType);
        if (isTourImage || isImage)
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
            height,
            fileId);

        if (isTourImage)
            await AddTourPreviewsAsync(request, storageKey, mediaFile, ct);
        else if (isImage)
            await AddLegacyThumbnailsAsync(request, storageKey, mediaFile, width!.Value, ct);

        await repository.AddAsync(mediaFile, ct);
        await repository.SaveChangesAsync(ct);

        return ToResponse(mediaFile, isTourImage ? false : null);
    }

    private async Task<UploadMediaResponse> UploadAvatarAsync(
        UploadMediaCommand request,
        Guid fileId,
        string storageKey,
        CancellationToken ct)
    {
        // Validate the bytes are a real image before mutating.
        _ = await ReadDimensionsAsync(request.FileContent, ct);
        if (request.FileContent.CanSeek)
            request.FileContent.Position = 0;

        var avatar = await imageProcessor.CreateSquareAvatarAsync(
            request.FileContent,
            AvatarSizes.SidePixels,
            ct);
        await using (avatar.Content)
        {
            var avatarKey = $"{storageKey}-avatar-{AvatarSizes.A256}";
            await storage.UploadAsync(avatar.Content, avatarKey, avatar.ContentType, ct);

            // Separate preview object so delete can remove both keys safely.
            if (avatar.Content.CanSeek)
                avatar.Content.Position = 0;
            var previewKey = $"{avatarKey}-preview-{AvatarSizes.A256}";
            await storage.UploadAsync(avatar.Content, previewKey, avatar.ContentType, ct);

            var mediaFile = MediaFile.Create(
                request.FileName,
                avatar.ContentType,
                avatar.Content.CanSeek ? avatar.Content.Length : request.SizeBytes,
                avatarKey,
                currentUser.UserId.ToString(),
                MediaPurposes.Avatar,
                avatar.Width,
                avatar.Height,
                fileId);
            mediaFile.AddThumbnail(previewKey, avatar.Width, avatar.Height, AvatarSizes.A256);
            mediaFile.MarkPublic();

            await repository.AddAsync(mediaFile, ct);
            await repository.SaveChangesAsync(ct);

            return ToResponse(mediaFile, true);
        }
    }

    private void EnsureAuthenticated()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Authentication is required.");
    }

    private void EnsureManagerOrAdmin()
    {
        EnsureAuthenticated();

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
        int originalWidth,
        CancellationToken ct)
    {
        foreach (var thumbWidth in ThumbnailWidthList.DistinctPositive(uploadOptions.Value.ThumbnailWidths))
        {
            if (originalWidth <= thumbWidth)
                continue;

            if (request.FileContent.CanSeek)
                request.FileContent.Position = 0;

            var resized = await imageProcessor.ResizeWithinAsync(request.FileContent, thumbWidth, ct);
            await using (resized.Content)
            {
                if (resized.Width <= 0 || resized.Height <= 0 || resized.Width > originalWidth)
                    continue;

                var thumbKey = $"{storageKey}-thumb-{thumbWidth}";
                await storage.UploadAsync(resized.Content, thumbKey, resized.ContentType, ct);
                mediaFile.AddThumbnail(thumbKey, resized.Width, resized.Height);
            }
        }
    }

    private static UploadMediaResponse ToResponse(MediaFile mediaFile, bool? isPublic)
    {
        var thumbnailResponses = new List<ThumbnailResponse>(mediaFile.Thumbnails.Count);
        foreach (var thumb in mediaFile.Thumbnails)
        {
            thumbnailResponses.Add(new ThumbnailResponse(
                CreateDeterministicGuid(thumb.StorageKey),
                thumb.Width,
                thumb.Height));
        }

        return new UploadMediaResponse(
            mediaFile.Id,
            mediaFile.OriginalFileName,
            mediaFile.ContentType,
            mediaFile.SizeBytes,
            thumbnailResponses,
            mediaFile.UploadedAt,
            mediaFile.Width,
            mediaFile.Height,
            isPublic);
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return new Guid(hash.AsSpan(0, 16));
    }
}
