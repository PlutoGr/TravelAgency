using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class UploadMediaCommandHandlerTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IImageProcessingService _imageProcessor = Substitute.For<IImageProcessingService>();
    private readonly IMediaFileRepository _repository = Substitute.For<IMediaFileRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly StorageSettings _storageSettings = new()
    {
        PresignTtlMinutes = 60
    };

    private readonly UploadSettings _uploadSettings = new()
    {
        MaxFileSizeBytes = 10 * 1024 * 1024,
        AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"],
        ThumbnailWidths = [200, 800]
    };

    private readonly UploadMediaCommandHandler _handler;

    public UploadMediaCommandHandlerTests()
    {
        _currentUser.UserId.Returns("user-123");

        _handler = new UploadMediaCommandHandler(
            _storage,
            _imageProcessor,
            _repository,
            _currentUser,
            Options.Create(_storageSettings),
            Options.Create(_uploadSettings));
    }

    [Fact]
    public async Task Handle_NonImageFile_UploadsAndReturnsResponseWithNoThumbnails()
    {
        var fileContent = new MemoryStream([1, 2, 3]);
        var command = new UploadMediaCommand(fileContent, "document.pdf", "application/pdf", 3);
        var expectedUrl = "https://storage/document.pdf?signed";

        _imageProcessor.IsImage("application/pdf").Returns(false);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Url.Should().Be(expectedUrl);
        result.FileName.Should().Be("document.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.SizeBytes.Should().Be(3);
        result.Thumbnails.Should().BeEmpty();
        result.UploadedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ImageFile_GeneratesThumbnailsForEachConfiguredWidth()
    {
        var fileContent = new MemoryStream(new byte[100]);
        var command = new UploadMediaCommand(fileContent, "photo.jpg", "image/jpeg", 100);

        _imageProcessor.IsImage("image/jpeg").Returns(true);
        _imageProcessor.ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<Stream>(new MemoryStream(new byte[50])));
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://storage/signed-url");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Thumbnails.Should().HaveCount(2);
        result.Thumbnails.Should().Contain(t => t.Width == 200);
        result.Thumbnails.Should().Contain(t => t.Width == 800);
    }

    [Fact]
    public async Task Handle_ImageFile_UploadsThumbForEachWidth()
    {
        var fileContent = new MemoryStream(new byte[100]);
        var command = new UploadMediaCommand(fileContent, "photo.jpg", "image/jpeg", 100);

        _imageProcessor.IsImage("image/jpeg").Returns(true);
        _imageProcessor.ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<Stream>(new MemoryStream(new byte[50])));
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://storage/signed");

        await _handler.Handle(command, CancellationToken.None);

        // Original + 2 thumbnails = 3 upload calls
        await _storage.Received(3).UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PdfFile_DoesNotGenerateThumbnails()
    {
        var fileContent = new MemoryStream([1, 2, 3]);
        var command = new UploadMediaCommand(fileContent, "file.pdf", "application/pdf", 3);

        _imageProcessor.IsImage("application/pdf").Returns(false);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://storage/signed");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Thumbnails.Should().BeEmpty();
        _imageProcessor.DidNotReceive().IsImage(Arg.Is<string>(ct => ct != "application/pdf"));
        await _imageProcessor.DidNotReceive().ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CallsRepositoryAddAsyncAndSaveChangesAsync()
    {
        var fileContent = new MemoryStream([1]);
        var command = new UploadMediaCommand(fileContent, "file.pdf", "application/pdf", 1);

        _imageProcessor.IsImage("application/pdf").Returns(false);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://url");

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<TravelAgency.Media.Domain.Entities.MediaFile>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StorageKeyIncludesUserIdAndFileName()
    {
        var fileContent = new MemoryStream([1]);
        var command = new UploadMediaCommand(fileContent, "test.pdf", "application/pdf", 1);
        string? capturedKey = null;

        _imageProcessor.IsImage("application/pdf").Returns(false);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Do<string>(k => capturedKey = k), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(string.Empty));
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://url");

        await _handler.Handle(command, CancellationToken.None);

        capturedKey.Should().StartWith("user-123/");
        capturedKey.Should().EndWith("/test.pdf");
    }

    [Fact]
    public async Task Handle_ThumbnailResponses_HavePresignedUrls()
    {
        var fileContent = new MemoryStream(new byte[100]);
        var command = new UploadMediaCommand(fileContent, "photo.jpg", "image/jpeg", 100);
        var thumbUrl = "https://storage/thumb-signed";

        _imageProcessor.IsImage("image/jpeg").Returns(true);
        _imageProcessor.ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(new MemoryStream(new byte[50])));
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://storage/main-signed", thumbUrl, thumbUrl);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Thumbnails.Should().AllSatisfy(t => t.Url.Should().NotBeNullOrEmpty());
    }
}
