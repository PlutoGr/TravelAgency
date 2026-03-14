using TravelAgency.Media.Application.Features.Delete;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class DeleteMediaCommandHandlerTests
{
    private readonly IMediaFileRepository _repository = Substitute.For<IMediaFileRepository>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly DeleteMediaCommandHandler _handler;

    public DeleteMediaCommandHandlerTests()
    {
        _currentUser.UserId.Returns("owner-user");
        _handler = new DeleteMediaCommandHandler(_repository, _storage, _currentUser);
    }

    private static MediaFile CreateActiveFile(string ownerId = "owner-user") =>
        MediaFile.Create("photo.jpg", "image/jpeg", 1024, "user/key/photo.jpg", ownerId);

    [Fact]
    public async Task Handle_ExistingOwnedFile_CallsMarkAsDeletedAndSaveChanges()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = CreateActiveFile();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None);

        mediaFile.Status.Should().Be(TravelAgency.Media.Domain.Enums.MediaFileStatus.Deleted);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingFile_DeletesMainStorageKey()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = CreateActiveFile();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None);

        await _storage.Received(1).DeleteAsync(mediaFile.StorageKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FileWithThumbnails_DeletesAllThumbnailStorageKeys()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = CreateActiveFile();
        mediaFile.AddThumbnail("key-thumb-200", 200, 0);
        mediaFile.AddThumbnail("key-thumb-800", 800, 0);
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None);

        await _storage.Received(1).DeleteAsync("key-thumb-200", Arg.Any<CancellationToken>());
        await _storage.Received(1).DeleteAsync("key-thumb-800", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FileWithThumbnails_DeletesMainPlusThumbnails()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = CreateActiveFile();
        mediaFile.AddThumbnail("key-thumb-200", 200, 0);
        mediaFile.AddThumbnail("key-thumb-800", 800, 0);
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None);

        await _storage.Received(3).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        var act = async () => await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeletedFile_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = CreateActiveFile();
        mediaFile.MarkAsDeleted();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        var act = async () => await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_DifferentOwner_ThrowsMediaAccessDeniedException()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = CreateActiveFile(ownerId: "other-user");
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        var act = async () => await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None);

        await act.Should().ThrowAsync<MediaAccessDeniedException>();
    }

    [Fact]
    public async Task Handle_DifferentOwner_DoesNotDeleteFromStorage()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = CreateActiveFile(ownerId: "other-user");
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        try { await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None); } catch { }

        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_DoesNotCallStorage()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        try { await _handler.Handle(new DeleteMediaCommand(fileId), CancellationToken.None); } catch { }

        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
