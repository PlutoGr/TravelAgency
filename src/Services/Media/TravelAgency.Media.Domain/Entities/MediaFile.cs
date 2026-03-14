using TravelAgency.Media.Domain.Enums;

namespace TravelAgency.Media.Domain.Entities;

public sealed class MediaFile
{
    private readonly List<MediaFileThumbnail> _thumbnails = [];

    public Guid Id { get; private set; }
    public string OriginalFileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = default!;
    public string OwnerId { get; private set; } = default!;
    public MediaFileStatus Status { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public IReadOnlyList<MediaFileThumbnail> Thumbnails => _thumbnails.AsReadOnly();

    private MediaFile() { }

    public static MediaFile Create(
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string ownerId)
    {
        return new MediaFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = originalFileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            OwnerId = ownerId,
            Status = MediaFileStatus.Active,
            UploadedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddThumbnail(string storageKey, int width, int height)
    {
        _thumbnails.Add(new MediaFileThumbnail(storageKey, width, height));
    }

    public void MarkAsDeleted()
    {
        Status = MediaFileStatus.Deleted;
    }
}
