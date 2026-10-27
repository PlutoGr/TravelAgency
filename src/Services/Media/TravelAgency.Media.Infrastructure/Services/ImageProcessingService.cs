using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Services;

namespace TravelAgency.Media.Infrastructure.Services;

public sealed class ImageProcessingService : IImageProcessingService
{
    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private static readonly JpegEncoder AvatarJpegEncoder = new() { Quality = 85 };

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

    public async Task<ImageDimensions> GetDimensionsAsync(Stream source, CancellationToken ct = default)
    {
        if (source.CanSeek)
            source.Position = 0;

        var info = await Image.IdentifyAsync(source, ct);
        if (source.CanSeek)
            source.Position = 0;

        if (info is null || info.Width <= 0 || info.Height <= 0)
            throw new InvalidOperationException("Image format is not recognized.");

        return new ImageDimensions(info.Width, info.Height);
    }

    public async Task<ResizedImage> ResizeWithinAsync(Stream source, int maxWidth, CancellationToken ct = default)
    {
        if (source.CanSeek)
            source.Position = 0;

        using var image = await Image.LoadAsync(source, ct);
        var (width, height) = PreviewSizer.FitWithin(image.Width, image.Height, maxWidth);
        if (width != image.Width || height != image.Height)
            image.Mutate(x => x.Resize(width, height));

        var format = image.Metadata.DecodedImageFormat
            ?? throw new InvalidOperationException("Image format is not recognized.");

        var ms = new MemoryStream();
        await image.SaveAsync(ms, format, ct);
        ms.Position = 0;
        return new ResizedImage(ms, width, height, format.DefaultMimeType);
    }

    public async Task<ResizedImage> CreateSquareAvatarAsync(Stream source, int sidePixels, CancellationToken ct = default)
    {
        if (source.CanSeek)
            source.Position = 0;

        using var image = await Image.LoadAsync(source, ct);

        var side = Math.Min(image.Width, image.Height);
        if (side <= 0)
            throw new InvalidOperationException("Image dimensions are empty.");

        var cropX = (image.Width - side) / 2;
        var cropY = (image.Height - side) / 2;
        image.Mutate(x =>
        {
            x.Crop(new Rectangle(cropX, cropY, side, side));
            if (side != sidePixels)
                x.Resize(sidePixels, sidePixels);
        });

        // Strip location and other metadata before save.
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, AvatarJpegEncoder, ct);
        ms.Position = 0;
        return new ResizedImage(ms, sidePixels, sidePixels, "image/jpeg");
    }
}
