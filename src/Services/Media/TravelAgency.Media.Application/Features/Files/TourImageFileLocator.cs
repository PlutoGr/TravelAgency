using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Enums;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Application.Features.Files;

internal static class TourImageFileLocator
{
    public static async Task<MediaFile> RequireActiveTourImage(
        IMediaFileRepository repository,
        Guid id,
        CancellationToken ct)
    {
        var file = await repository.GetByIdAsync(id, ct);
        if (file is null
            || file.Status == MediaFileStatus.Deleted
            || !MediaPurposes.IsTourImage(file.Purpose))
        {
            throw new MediaNotFoundException(id);
        }

        return file;
    }

    public static MediaFileThumbnail RequirePreview(MediaFile file, string size)
    {
        if (!TourImageSizes.IsKnown(size))
            throw new MediaNotFoundException(file.Id);

        var thumb = file.Thumbnails.FirstOrDefault(t =>
            string.Equals(t.SizeCode, size, StringComparison.Ordinal));
        if (thumb is null)
            throw new MediaNotFoundException(file.Id);

        return thumb;
    }
}
