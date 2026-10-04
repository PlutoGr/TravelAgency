namespace TravelAgency.Media.Domain.Entities;

public sealed class MediaFileThumbnail
{
    public string StorageKey { get; private set; } = default!;
    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>Tour preview slot, for example w200. Null for legacy thumbnails.</summary>
    public string? SizeCode { get; private set; }

    private MediaFileThumbnail() { }

    public MediaFileThumbnail(string storageKey, int width, int height, string? sizeCode = null)
    {
        StorageKey = storageKey;
        Width = width;
        Height = height;
        SizeCode = sizeCode;
    }
}
