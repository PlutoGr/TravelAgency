using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Services;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class UploadImageDimensionTests
{
    private readonly MemoryStorage _storage = new();
    private readonly IMediaFileRepository _repository = Substitute.For<IMediaFileRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private MediaFile? _saved;

    public UploadImageDimensionTests()
    {
        _currentUser.UserId.Returns(Guid.Parse("00000000-0000-0000-0000-000000000123"));
        _repository.AddAsync(Arg.Do<MediaFile>(file => _saved = file), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_DuplicateThumbnailWidths_StoresEachSizeOnce()
    {
        var png = await CreatePngAsync(1000, 500);
        var handler = CreateHandler([200, 800, 200, 800]);

        var result = await handler.Handle(
            new UploadMediaCommand(png, "photo.png", "image/png", png.Length),
            CancellationToken.None);

        result.Thumbnails.Select(thumb => thumb.Width).Should().Equal(200, 800);
        _saved!.Thumbnails.Select(thumb => thumb.Width).Should().Equal(200, 800);
        _storage.Objects.Keys.Should().HaveCount(3);
        _storage.UploadsByKey.Values.Should().OnlyContain(count => count == 1);
        _saved.Thumbnails.Select(thumb => thumb.StorageKey).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_Image_StoresOriginalAndThumbnailDimensionsFromTheSameFile()
    {
        var png = await CreatePngAsync(1000, 500);
        var handler = CreateHandler([200, 800]);

        var result = await handler.Handle(
            new UploadMediaCommand(png, "photo.png", "image/png", png.Length),
            CancellationToken.None);

        result.Width.Should().Be(1000);
        result.Height.Should().Be(500);
        _saved!.Width.Should().Be(result.Width);
        _saved.Height.Should().Be(result.Height);
        result.Thumbnails.Select(thumb => (thumb.Width, thumb.Height)).Should().Equal((200, 100), (800, 400));

        foreach (var thumb in _saved.Thumbnails)
        {
            var stored = Identify(_storage.Objects[thumb.StorageKey]);
            stored.Width.Should().Be(thumb.Width);
            stored.Height.Should().Be(thumb.Height);
        }
    }

    [Fact]
    public async Task Handle_ImageNotWiderThanThumbnail_DoesNotCreateOrUpscaleIt()
    {
        var png = await CreatePngAsync(200, 200);
        var handler = CreateHandler([200, 800]);

        var result = await handler.Handle(
            new UploadMediaCommand(png, "small.png", "image/png", png.Length),
            CancellationToken.None);

        result.Width.Should().Be(200);
        result.Height.Should().Be(200);
        result.Thumbnails.Should().BeEmpty();
        _saved!.Thumbnails.Should().BeEmpty();
        _storage.Objects.Should().ContainSingle();

        var stored = Identify(_storage.Objects.Values.Single());
        stored.Width.Should().Be(200);
        stored.Height.Should().Be(200);
    }

    [Fact]
    public async Task Handle_ImageBetweenThumbnailWidths_SkipsOnlyTheLargerOne()
    {
        var png = await CreatePngAsync(500, 250);
        var handler = CreateHandler([200, 800]);

        var result = await handler.Handle(
            new UploadMediaCommand(png, "mid.png", "image/png", png.Length),
            CancellationToken.None);

        result.Thumbnails.Select(thumb => (thumb.Width, thumb.Height)).Should().Equal([(200, 100)]);
        _storage.Objects.Keys.Should().NotContain(key => key.EndsWith("-thumb-800", StringComparison.Ordinal));
    }

    private UploadMediaCommandHandler CreateHandler(int[] thumbnailWidths)
    {
        var settings = new UploadSettings
        {
            MaxFileSizeBytes = 10 * 1024 * 1024,
            AllowedMimeTypes = ["image/png"],
            ThumbnailWidths = thumbnailWidths
        };

        return new UploadMediaCommandHandler(
            _storage,
            new ImageProcessingService(),
            _repository,
            _currentUser,
            Options.Create(settings));
    }

    private static async Task<MemoryStream> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private static (int Width, int Height) Identify(byte[] bytes)
    {
        var info = Image.Identify(new MemoryStream(bytes));
        info.Should().NotBeNull();
        return (info!.Width, info.Height);
    }

    private sealed class MemoryStorage : IStorageService
    {
        public Dictionary<string, byte[]> Objects { get; } = new();
        public Dictionary<string, int> UploadsByKey { get; } = new();

        public async Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken ct = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            Objects[key] = buffer.ToArray();
            UploadsByKey[key] = UploadsByKey.GetValueOrDefault(key) + 1;
            return key;
        }

        public Task<Stream> DownloadAsync(string key, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream(Objects[key]));

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            Objects.Remove(key);
            return Task.CompletedTask;
        }

        public Task<string> GeneratePresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default) =>
            Task.FromResult("https://storage.example/signed");
    }
}
