using TravelAgency.Media.Application.Features.Get;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class GetMediaQueryHandlerTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

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
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "user/key/photo.jpg", OwnerId.ToString());
        var expectedStream = new MemoryStream([1, 2, 3]);

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.DownloadAsync(mediaFile.StorageKey, Arg.Any<CancellationToken>()).Returns(expectedStream);

        var result = await _handler.Handle(Query(fileId, OwnerId, AppRoles.Client), CancellationToken.None);

        result.Should().NotBeNull();
        result.Content.Should().BeSameAs(expectedStream);
        result.ContentType.Should().Be("image/jpeg");
        result.FileName.Should().Be("photo.jpg");
        result.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PublicFile_OtherClient_ReturnsStream()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", OwnerId.ToString());
        mediaFile.MarkPublic();
        var expectedStream = new MemoryStream([4, 5]);

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.DownloadAsync(mediaFile.StorageKey, Arg.Any<CancellationToken>()).Returns(expectedStream);

        var result = await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None);

        result.Content.Should().BeSameAs(expectedStream);
        result.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Owner_PrivateFile_ReturnsStream()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = PrivateFile();
        var expectedStream = new MemoryStream([7]);

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.DownloadAsync(mediaFile.StorageKey, Arg.Any<CancellationToken>()).Returns(expectedStream);

        var result = await _handler.Handle(Query(fileId, OwnerId, AppRoles.Client), CancellationToken.None);

        result.Content.Should().BeSameAs(expectedStream);
        result.IsPublic.Should().BeFalse();
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Admin)]
    public async Task Handle_ManagerOrAdmin_OtherPrivateFile_ReturnsStream(string role)
    {
        var fileId = Guid.NewGuid();
        var mediaFile = PrivateFile();
        var expectedStream = new MemoryStream([8]);

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.DownloadAsync(mediaFile.StorageKey, Arg.Any<CancellationToken>()).Returns(expectedStream);

        var result = await _handler.Handle(Query(fileId, OtherId, role), CancellationToken.None);

        result.Content.Should().BeSameAs(expectedStream);
        result.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OtherClient_PrivateFile_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(PrivateFile());

        var act = async () => await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_OtherClient_PrivateFile_SameExceptionAsMissingId()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(PrivateFile());

        var denied = async () => await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None);
        var deniedEx = await denied.Should().ThrowAsync<MediaNotFoundException>();

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);
        var missing = async () => await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None);
        var missingEx = await missing.Should().ThrowAsync<MediaNotFoundException>();

        deniedEx.Which.Message.Should().Be(missingEx.Which.Message);
        deniedEx.Which.Should().BeOfType<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_OtherClient_PrivateFile_DoesNotCallStorage()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(PrivateFile());

        try { await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None); } catch { }

        await _storage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        var act = async () => await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedFile_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = PrivateFile();
        mediaFile.MarkAsDeleted();

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        var act = async () => await _handler.Handle(Query(fileId, OwnerId, AppRoles.Admin), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedPublicFile_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = PrivateFile();
        mediaFile.MarkPublic();
        mediaFile.MarkAsDeleted();

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        var act = async () => await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
        await _storage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeletedFile_DoesNotCallStorage()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = PrivateFile();
        mediaFile.MarkAsDeleted();

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        try { await _handler.Handle(Query(fileId, OwnerId, AppRoles.Client), CancellationToken.None); } catch { }

        await _storage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFoundException_ContainsFileId()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        var act = async () => await _handler.Handle(Query(fileId, OtherId, AppRoles.Client), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<MediaNotFoundException>();
        ex.Which.Message.Should().Contain(fileId.ToString());
    }

    private static MediaFile PrivateFile() =>
        MediaFile.Create("draft.pdf", "application/pdf", 32, "owners/draft.pdf", OwnerId.ToString());

    private static GetMediaQuery Query(Guid fileId, Guid userId, params string[] roles) =>
        new(fileId, userId, roles);
}
