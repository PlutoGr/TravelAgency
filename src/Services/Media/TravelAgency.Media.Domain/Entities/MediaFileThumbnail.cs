namespace TravelAgency.Media.Domain.Entities;

public sealed class MediaFileThumbnail
{
    public string StorageKey { get; }
    public int Width { get; }
    public int Height { get; }

    public MediaFileThumbnail(string storageKey, int width, int height)
    {
        StorageKey = storageKey;
        Width = width;
        Height = height;
    }
}
