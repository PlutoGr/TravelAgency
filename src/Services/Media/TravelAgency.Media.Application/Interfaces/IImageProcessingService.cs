namespace TravelAgency.Media.Application.Interfaces;

public readonly record struct ImageDimensions(int Width, int Height);

public sealed record ResizedImage(Stream Content, int Width, int Height, string ContentType);

public interface IImageProcessingService
{
    bool IsImage(string contentType);
    Task<Stream> ResizeAsync(Stream source, int width, CancellationToken ct = default);
    Task<ImageDimensions> GetDimensionsAsync(Stream source, CancellationToken ct = default);

    /// <summary>
    /// Scales the image so its width is at most <paramref name="maxWidth"/>.
    /// Never enlarges. Aspect ratio is kept.
    /// </summary>
    Task<ResizedImage> ResizeWithinAsync(Stream source, int maxWidth, CancellationToken ct = default);
}
