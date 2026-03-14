namespace TravelAgency.Media.Application.Settings;

public sealed class UploadSettings
{
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    public string[] AllowedMimeTypes { get; init; } = ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"];
    public int[] ThumbnailWidths { get; init; } = [200, 800];
}
