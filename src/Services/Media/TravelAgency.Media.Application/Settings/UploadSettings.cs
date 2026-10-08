namespace TravelAgency.Media.Application.Settings;

public sealed class UploadSettings
{
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    public string[] AllowedMimeTypes { get; init; } = ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"];

    /// <summary>
    /// Preview widths in pixels. Starts empty: the options binder appends configuration
    /// onto a non-empty array, so a baked-in [200, 800] plus appsettings repeated each size.
    /// </summary>
    public int[] ThumbnailWidths { get; set; } = [];
}
