using FluentAssertions;
using Moq;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Auth.Commands.Logout;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_refreshTokenRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTokenBelongsToCorrectUser_RevokesToken()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7));

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _refreshTokenRepoMock.Setup(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new LogoutCommand("valid-token", userId), CancellationToken.None);

        token.IsRevoked.Should().BeTrue();
        _refreshTokenRepoMock.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsEarlyWithoutError()
    {
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = async () => await _handler.Handle(
            new LogoutCommand("nonexistent", Guid.NewGuid()), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // FIX-009: Ownership check — token must belong to the requesting user

    [Fact]
    public async Task Handle_WhenTokenBelongsToDifferentUser_ThrowsUnauthorizedException()
    {
        var tokenOwner = Guid.NewGuid();
        var requestingUser = Guid.NewGuid();
        var token = RefreshToken.Create(tokenOwner, "some-token", DateTime.UtcNow.AddDays(7));

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("some-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var act = async () => await _handler.Handle(
            new LogoutCommand("some-token", requestingUser), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyRevoked_DoesNotCallUpdate()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "revoked-token", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("revoked-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        await _handler.Handle(new LogoutCommand("revoked-token", userId), CancellationToken.None);

        _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_DoesNotCallUpdate()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "expired-token", DateTime.UtcNow.AddSeconds(-1));

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("expired-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        await _handler.Handle(new LogoutCommand("expired-token", userId), CancellationToken.None);

        _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
