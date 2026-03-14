using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TravelAgency.Media.Application.Interfaces;

namespace TravelAgency.Media.Infrastructure.Services;

public sealed class ImageProcessingService : IImageProcessingService
{
    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    public bool IsImage(string contentType) =>
        ImageMimeTypes.Contains(contentType);

    public async Task<Stream> ResizeAsync(Stream source, int width, CancellationToken ct = default)
    {
        var image = await Image.LoadAsync(source, ct);
        var aspectRatio = (double)image.Height / image.Width;
        var height = (int)(width * aspectRatio);

        image.Mutate(x => x.Resize(width, height));

        var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }
}
