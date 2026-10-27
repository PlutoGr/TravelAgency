using FluentAssertions;
using Moq;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile.Commands.UploadAvatar;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class UploadAvatarCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAvatarMediaClient> _media = new();
    private readonly UploadAvatarCommandHandler _handler;

    public UploadAvatarCommandHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UploadAvatarCommandHandler(_currentUser.Object, _users.Object, _uow.Object, _media.Object);
    }

    [Fact]
    public async Task Handle_UploadsAndSetsAvatarUrl()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "A", "B", null);
        var mediaId = Guid.NewGuid();
        _currentUser.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUser.Setup(s => s.UserId).Returns(userId);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _media.Setup(m => m.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvatarUploadResult(mediaId));

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await _handler.Handle(
            new UploadAvatarCommand(stream, "a.png", "image/png", 3, "token"),
            CancellationToken.None);

        result.AvatarUrl.Should().Be($"/api/v1/media/files/{mediaId:D}/a256");
        user.AvatarMediaFileId.Should().Be(mediaId);
    }

    [Fact]
    public async Task Handle_ReplacesPreviousAvatar_DeletesOld()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "A", "B", null);
        var oldId = Guid.NewGuid();
        user.SetAvatar(oldId);
        var newId = Guid.NewGuid();
        _currentUser.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUser.Setup(s => s.UserId).Returns(userId);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _media.Setup(m => m.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvatarUploadResult(newId));

        await using var stream = new MemoryStream([1, 2, 3]);
        await _handler.Handle(new UploadAvatarCommand(stream, "a.png", "image/png", 3, "token"), CancellationToken.None);

        _media.Verify(m => m.DeleteAsync(oldId, "token", It.IsAny<CancellationToken>()), Times.Once);
        user.AvatarMediaFileId.Should().Be(newId);
    }

    [Fact]
    public async Task Handle_Unauthenticated_Throws()
    {
        _currentUser.Setup(s => s.IsAuthenticated).Returns(false);
        await using var stream = new MemoryStream([1]);
        var act = async () => await _handler.Handle(
            new UploadAvatarCommand(stream, "a.png", "image/png", 1, null), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
