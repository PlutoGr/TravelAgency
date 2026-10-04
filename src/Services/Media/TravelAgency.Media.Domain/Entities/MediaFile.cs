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

    /// <summary>
    /// False until a service calls MarkMediaFilesPublic. There is no reverse transition:
    /// a cover that was once published must stay readable for favorites of an unpublished tour.
    /// </summary>
    public bool IsPublic { get; private set; }

    /// <summary>Pixel width of the original. Null for files uploaded before tour images.</summary>
    public int? Width { get; private set; }

    /// <summary>Pixel height of the original. Null for files uploaded before tour images.</summary>
    public int? Height { get; private set; }

    /// <summary><see cref="MediaPurposes"/>. Defaults to general so existing rows are not tour images.</summary>
    public string Purpose { get; private set; } = MediaPurposes.General;

    public IReadOnlyList<MediaFileThumbnail> Thumbnails => _thumbnails.AsReadOnly();

    private MediaFile() { }

    public static MediaFile Create(
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        string ownerId,
        string purpose = MediaPurposes.General,
        int? width = null,
        int? height = null)
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
            UploadedAt = DateTimeOffset.UtcNow,
            IsPublic = false,
            Purpose = string.IsNullOrWhiteSpace(purpose) ? MediaPurposes.General : purpose,
            Width = width,
            Height = height
        };
    }

    public void AddThumbnail(string storageKey, int width, int height, string? sizeCode = null)
    {
        _thumbnails.Add(new MediaFileThumbnail(storageKey, width, height, sizeCode));
    }

    /// <summary>
    /// One-way publication. Repeated calls stay public. Nothing in the model sets the flag back to false.
    /// </summary>
    public void MarkPublic()
    {
        IsPublic = true;
    }

    public void MarkAsDeleted()
    {
        Status = MediaFileStatus.Deleted;
    }
}
