using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using TravelAgency.Media.Infrastructure.Services;

namespace TravelAgency.Media.UnitTests.Infrastructure;

public class ImageProcessingServiceTests
{
    private readonly ImageProcessingService _service = new();

    [Fact]
    public async Task ResizeWithinAsync_ScalesDown_AndKeepsAspectRatio()
    {
        await using var source = await CreatePngAsync(400, 200);

        var resized = await _service.ResizeWithinAsync(source, 200);

        resized.Width.Should().Be(200);
        resized.Height.Should().Be(100);
        resized.ContentType.Should().Be("image/png");

        var info = await Image.IdentifyAsync(resized.Content);
        info.Should().NotBeNull();
        info!.Width.Should().Be(200);
        info.Height.Should().Be(100);
    }

    [Fact]
    public async Task ResizeWithinAsync_DoesNotUpscaleBeyondOriginal()
    {
        await using var source = await CreatePngAsync(120, 80);

        var resized = await _service.ResizeWithinAsync(source, 1600);

        resized.Width.Should().Be(120);
        resized.Height.Should().Be(80);

        var info = await Image.IdentifyAsync(resized.Content);
        info!.Width.Should().Be(120);
        info.Height.Should().Be(80);
    }

    [Fact]
    public async Task GetDimensionsAsync_ReturnsOriginalSize()
    {
        await using var source = await CreatePngAsync(64, 32);

        var dimensions = await _service.GetDimensionsAsync(source);

        dimensions.Width.Should().Be(64);
        dimensions.Height.Should().Be(32);
    }

    [Fact]
    public async Task CreateSquareAvatarAsync_CropsTo256Jpeg_AndStripsExif()
    {
        await using var source = await CreatePngAsync(400, 200);

        var avatar = await _service.CreateSquareAvatarAsync(source, 256);
        await using (avatar.Content)
        {
            avatar.Width.Should().Be(256);
            avatar.Height.Should().Be(256);
            avatar.ContentType.Should().Be("image/jpeg");

            using var loaded = await Image.LoadAsync(avatar.Content);
            loaded.Width.Should().Be(256);
            loaded.Height.Should().Be(256);
            loaded.Metadata.ExifProfile.Should().BeNull();
        }
    }

    [Fact]
    public async Task CreateSquareAvatarAsync_KeepsSquareCorners_AndCentersNonSquare()
    {
        await using var square = await CreatePaintedPngAsync(256, 256, image =>
        {
            for (var y = 0; y < 48; y++)
            {
                for (var x = 0; x < 48; x++)
                    image[x, y] = new Rgba32(220, 10, 10);
            }
        });

        var squareAvatar = await _service.CreateSquareAvatarAsync(square, 256);
        await using (squareAvatar.Content)
        {
            squareAvatar.Width.Should().Be(256);
            squareAvatar.Height.Should().Be(256);
            using var loaded = await Image.LoadAsync<Rgba32>(squareAvatar.Content);
            var corner = loaded[8, 8];
            corner.R.Should().BeGreaterThan(180);
            corner.B.Should().BeLessThan(80);
            var center = loaded[128, 128];
            center.B.Should().BeGreaterThan(180);
            center.R.Should().BeLessThan(80);
        }

        await using var wide = await CreatePaintedPngAsync(400, 200, image =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                    image[x, y] = x < 200 ? new Rgba32(220, 10, 10) : new Rgba32(10, 10, 220);
            }
        });

        var wideAvatar = await _service.CreateSquareAvatarAsync(wide, 256);
        await using (wideAvatar.Content)
        {
            wideAvatar.Width.Should().Be(256);
            wideAvatar.Height.Should().Be(256);
            wideAvatar.ContentType.Should().Be("image/jpeg");
            using var loaded = await Image.LoadAsync<Rgba32>(wideAvatar.Content);
            var left = loaded[16, 128];
            var right = loaded[240, 128];
            left.R.Should().BeGreaterThan(150);
            left.B.Should().BeLessThan(80);
            right.B.Should().BeGreaterThan(150);
            right.R.Should().BeLessThan(80);
        }
    }

    [Fact]
    public async Task CreateSquareAvatarAsync_AppliesExifOrientation_BeforeCrop()
    {
        // 200x100 landscape pixels tagged Rotate90 → displayed as 100x200 portrait.
        await using var source = await CreateJpegWithOrientationAsync(200, 100, ExifOrientationMode.RightTop);

        var avatar = await _service.CreateSquareAvatarAsync(source, 256);
        await using (avatar.Content)
        {
            avatar.Width.Should().Be(256);
            avatar.Height.Should().Be(256);
            using var loaded = await Image.LoadAsync(avatar.Content);
            loaded.Width.Should().Be(256);
            loaded.Height.Should().Be(256);
            loaded.Metadata.ExifProfile.Should().BeNull();
        }
    }

    private static Task<MemoryStream> CreatePngAsync(int width, int height) =>
        CreatePaintedPngAsync(width, height, image =>
        {
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                    image[x, y] = new Rgba32(10, 10, 220);
            }
        });

    private static async Task<MemoryStream> CreatePaintedPngAsync(
        int width,
        int height,
        Action<Image<Rgba32>> paint)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(10, 10, 220));
        paint(image);
        var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        ms.Position = 0;
        return ms;
    }

    private static async Task<MemoryStream> CreateJpegWithOrientationAsync(int width, int height, ushort orientation)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation);
        var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 90 });
        ms.Position = 0;
        return ms;
    }
}
