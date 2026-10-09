using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Services;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class UploadMediaCommandHandlerTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IImageProcessingService _imageProcessor = Substitute.For<IImageProcessingService>();
    private readonly IMediaFileRepository _repository = Substitute.For<IMediaFileRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly UploadSettings _uploadSettings = new()
    {
        MaxFileSizeBytes = 10 * 1024 * 1024,
        AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"],
        ThumbnailWidths = [200, 800]
    };

    private readonly UploadMediaCommandHandler _handler;

    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000123");

    public UploadMediaCommandHandlerTests()
    {
        _currentUser.UserId.Returns(TestUserId);

        _handler = new UploadMediaCommandHandler(
            _storage,
            _imageProcessor,
            _repository,
            _currentUser,
            Options.Create(_uploadSettings));
    }

    [Fact]
    public async Task Handle_NonImageFile_UploadsAndReturnsResponseWithNoThumbnails()
    {
        var fileContent = new MemoryStream([1, 2, 3]);
        var command = new UploadMediaCommand(fileContent, "document.pdf", "application/pdf", 3);

        _imageProcessor.IsImage("application/pdf").Returns(false);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("http://minio:9000/media/document.pdf?X-Amz-Signature=abc");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
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

        SetupRasterImage("image/jpeg", 1600, 900);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Width.Should().Be(1600);
        result.Height.Should().Be(900);
        result.Thumbnails.Should().HaveCount(2);
        result.Thumbnails.Select(t => (t.Width, t.Height)).Should().Equal((200, 113), (800, 450));
    }

    [Fact]
    public async Task Handle_ImageFile_UploadsThumbForEachWidth()
    {
        var fileContent = new MemoryStream(new byte[100]);
        var command = new UploadMediaCommand(fileContent, "photo.jpg", "image/jpeg", 100);

        SetupRasterImage("image/jpeg", 1600, 900);

        await _handler.Handle(command, CancellationToken.None);

        // Original + 2 thumbnails = 3 upload calls
        await _storage.Received(3).UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _imageProcessor.DidNotReceive().ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PdfFile_DoesNotGenerateThumbnails()
    {
        var fileContent = new MemoryStream([1, 2, 3]);
        var command = new UploadMediaCommand(fileContent, "file.pdf", "application/pdf", 3);

        _imageProcessor.IsImage("application/pdf").Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Thumbnails.Should().BeEmpty();
        result.Width.Should().BeNull();
        result.Height.Should().BeNull();
        _imageProcessor.DidNotReceive().IsImage(Arg.Is<string>(ct => ct != "application/pdf"));
        await _imageProcessor.DidNotReceive().ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _imageProcessor.DidNotReceive().GetDimensionsAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CallsRepositoryAddAsyncAndSaveChangesAsync()
    {
        var fileContent = new MemoryStream([1]);
        var command = new UploadMediaCommand(fileContent, "file.pdf", "application/pdf", 1);

        _imageProcessor.IsImage("application/pdf").Returns(false);

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

        await _handler.Handle(command, CancellationToken.None);

        capturedKey.Should().StartWith($"{TestUserId:N}/");
        capturedKey.Should().EndWith("/test.pdf");
    }

    [Fact]
    public async Task Handle_ThumbnailResponses_HaveDimensionsWithoutStorageAddress()
    {
        var fileContent = new MemoryStream(new byte[100]);
        var command = new UploadMediaCommand(fileContent, "photo.jpg", "image/jpeg", 100);

        SetupRasterImage("image/jpeg", 1600, 900);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("http://minio:9000/media/photo.jpg?X-Amz-Signature=abc");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Thumbnails.Should().NotBeEmpty();
        result.Thumbnails.Should().OnlyContain(thumb => thumb.Width > 0 && thumb.Height > 0);
        var json = System.Text.Json.JsonSerializer.Serialize(result, WebJson());
        json.Should().NotContain("minio");
        json.Should().NotContain("X-Amz-");
        json.Should().NotContain("http://");
        json.Should().NotContain("https://");
    }

    [Fact]
    public async Task Handle_TourImage_StoresOwnerDimensionsAndPrivatePreviewsWithoutUpscaleOrPresign()
    {
        var fileContent = new MemoryStream(new byte[32]);
        var command = new UploadMediaCommand(fileContent, "cover.png", "image/png", 32, MediaPurposes.TourImage);
        MediaFile? saved = null;

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Role.Returns(AppRoles.Manager);
        _imageProcessor.GetDimensionsAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(new ImageDimensions(100, 50));
        _imageProcessor.ResizeWithinAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var maxWidth = ci.ArgAt<int>(1);
                var (width, height) = PreviewSizer.FitWithin(100, 50, maxWidth);
                return new ResizedImage(new MemoryStream([1, 2, 3]), width, height, "image/png");
            });
        _repository.AddAsync(Arg.Do<MediaFile>(f => saved = f), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.IsPublic.Should().BeFalse();
        saved.Purpose.Should().Be(MediaPurposes.TourImage);
        saved.OwnerId.Should().Be(TestUserId.ToString());
        saved.Width.Should().Be(100);
        saved.Height.Should().Be(50);
        saved.Thumbnails.Select(t => t.SizeCode).Should().Equal(
            TourImageSizes.W200, TourImageSizes.W800, TourImageSizes.W1600);
        saved.Thumbnails.Should().OnlyContain(t => t.Width == 100 && t.Height == 50);

        result.IsPublic.Should().BeFalse();
        result.Width.Should().Be(100);
        result.Height.Should().Be(50);
        result.Thumbnails.Should().HaveCount(3);
        var json = System.Text.Json.JsonSerializer.Serialize(result, WebJson());
        json.Should().NotContain("minio");
        json.Should().NotContain("X-Amz-");
        json.Should().NotContain("http://");
        json.Should().NotContain("https://");

        await _storage.DidNotReceive()
            .GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await _imageProcessor.DidNotReceive()
            .ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _imageProcessor.Received(1).ResizeWithinAsync(Arg.Any<Stream>(), 200, Arg.Any<CancellationToken>());
        await _imageProcessor.Received(1).ResizeWithinAsync(Arg.Any<Stream>(), 800, Arg.Any<CancellationToken>());
        await _imageProcessor.Received(1).ResizeWithinAsync(Arg.Any<Stream>(), 1600, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TourImage_ClientRole_ThrowsAccessDenied()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Role.Returns(AppRoles.Client);
        var command = new UploadMediaCommand(new MemoryStream([1]), "a.jpg", "image/jpeg", 1, MediaPurposes.TourImage);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<MediaAccessDeniedException>();
        await _storage.DidNotReceive()
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TourImage_Anonymous_ThrowsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns(Guid.Empty);
        var command = new UploadMediaCommand(new MemoryStream([1]), "a.jpg", "image/jpeg", 1, "tour-image");

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static System.Text.Json.JsonSerializerOptions WebJson() =>
        new(System.Text.Json.JsonSerializerDefaults.Web);

    private void SetupRasterImage(string contentType, int width, int height)
    {
        _imageProcessor.IsImage(contentType).Returns(true);
        _imageProcessor.GetDimensionsAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(new ImageDimensions(width, height));
        _imageProcessor.ResizeWithinAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var maxWidth = ci.ArgAt<int>(1);
                var (fittedWidth, fittedHeight) = PreviewSizer.FitWithin(width, height, maxWidth);
                return new ResizedImage(new MemoryStream(new byte[50]), fittedWidth, fittedHeight, contentType);
            });
    }
}
