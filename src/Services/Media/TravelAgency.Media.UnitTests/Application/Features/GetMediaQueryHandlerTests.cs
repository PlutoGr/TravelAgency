using TravelAgency.Media.Application.Features.Get;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class GetMediaQueryHandlerTests
{
    private readonly IMediaFileRepository _repository = Substitute.For<IMediaFileRepository>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly GetMediaQueryHandler _handler;

    public GetMediaQueryHandlerTests()
    {
        _handler = new GetMediaQueryHandler(_repository, _storage);
    }

    [Fact]
    public async Task Handle_ExistingActiveFile_ReturnsStreamAndContentType()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "user/key/photo.jpg", "user-1");
        var expectedStream = new MemoryStream([1, 2, 3]);

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.DownloadAsync(mediaFile.StorageKey, Arg.Any<CancellationToken>()).Returns(expectedStream);

        var result = await _handler.Handle(new GetMediaQuery(fileId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().BeSameAs(expectedStream);
        result.ContentType.Should().Be("image/jpeg");
        result.FileName.Should().Be("photo.jpg");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        var act = async () => await _handler.Handle(new GetMediaQuery(fileId), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedFile_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user-1");
        mediaFile.MarkAsDeleted();

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        var act = async () => await _handler.Handle(new GetMediaQuery(fileId), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedFile_DoesNotCallStorage()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user-1");
        mediaFile.MarkAsDeleted();

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        try { await _handler.Handle(new GetMediaQuery(fileId), CancellationToken.None); } catch { }

        await _storage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFoundException_ContainsFileId()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        var act = async () => await _handler.Handle(new GetMediaQuery(fileId), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<MediaNotFoundException>();
        ex.Which.Message.Should().Contain(fileId.ToString());
    }
}
