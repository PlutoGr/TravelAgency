using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Auth.Commands.RefreshToken;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtServiceMock = new();
    private readonly RefreshTokenCommandHandler _handler;

    private readonly JwtSettings _jwtSettings = new() { RefreshTokenExpirationDays = 7 };

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _userRepoMock.Object,
            _refreshTokenRepoMock.Object,
            _jwtServiceMock.Object,
            Options.Create(_jwtSettings));
    }

    [Fact]
    public async Task Handle_WithActiveToken_ReturnsNewAuthTokensDto()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "J", "D", null);
        var oldToken = RefreshToken.Create(userId, "old-refresh", DateTime.UtcNow.AddDays(7));
        var newRefreshStr = "new-refresh-token";
        var newAccessToken = "new.access.token";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("old-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldToken);
        _userRepoMock.Setup(r => r.GetByIdAsync(oldToken.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user))
            .Returns(new AuthTokensDto(newAccessToken, string.Empty, expiresAt));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns(newRefreshStr);
        _refreshTokenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new RefreshTokenCommand(new RefreshTokenRequest("old-refresh"));
        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRefreshStr);
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_WithActiveToken_RevokesOldTokenWithNewToken()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "J", "D", null);
        var oldToken = RefreshToken.Create(userId, "old-refresh", DateTime.UtcNow.AddDays(7));

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("old-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldToken);
        _userRepoMock.Setup(r => r.GetByIdAsync(oldToken.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto("t", string.Empty, DateTime.UtcNow.AddHours(1)));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new-rt");
        _refreshTokenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new RefreshTokenCommand(new RefreshTokenRequest("old-refresh")), CancellationToken.None);

        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByToken.Should().Be("new-rt");
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsUnauthorizedException()
    {
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = async () => await _handler.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest("nonexistent")),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenTokenInactive_ThrowsUnauthorizedException()
    {
        var expiredToken = RefreshToken.Create(Guid.NewGuid(), "expired-t", DateTime.UtcNow.AddSeconds(-1));

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("expired-t", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var act = async () => await _handler.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest("expired-t")),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenTokenRevoked_ThrowsUnauthorizedException()
    {
        var revokedToken = RefreshToken.Create(Guid.NewGuid(), "revoked-t", DateTime.UtcNow.AddDays(7));
        revokedToken.Revoke();

        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("revoked-t", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var act = async () => await _handler.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest("revoked-t")),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
