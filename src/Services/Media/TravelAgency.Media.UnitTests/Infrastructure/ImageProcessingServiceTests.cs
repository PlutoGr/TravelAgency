using SixLabors.ImageSharp;
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

    private static async Task<MemoryStream> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        ms.Position = 0;
        return ms;
    }
}
