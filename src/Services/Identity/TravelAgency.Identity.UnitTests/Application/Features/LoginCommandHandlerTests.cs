using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Auth.Commands.Login;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LoginCommandHandler _handler;

    private readonly JwtSettings _jwtSettings = new()
    {
        Issuer = "issuer",
        Audience = "audience",
        SigningKey = "key",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepoMock.Object,
            _refreshTokenRepoMock.Object,
            _jwtServiceMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            Options.Create(_jwtSettings));
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthTokensDto()
    {
        var user = User.Create("user@example.com", "hashedPw", "John", "Doe", null);
        var expectedAccessToken = "access.token.here";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var expectedRefreshToken = "refresh-token-base64";

        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("myPassword", "hashedPw")).Returns(true);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user))
            .Returns(new AuthTokensDto(expectedAccessToken, string.Empty, expiresAt));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns(expectedRefreshToken);
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new LoginCommand(new LoginRequest("user@example.com", "myPassword"));
        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be(expectedAccessToken);
        result.RefreshToken.Should().Be(expectedRefreshToken);
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new LoginCommand(new LoginRequest("notfound@example.com", "pass"));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ThrowsUnauthorizedException()
    {
        var user = User.Create("user@example.com", "hashedPw", "John", "Doe", null);
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("wrongPassword", "hashedPw")).Returns(false);

        var command = new LoginCommand(new LoginRequest("user@example.com", "wrongPassword"));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithValidCredentials_StagesRefreshToken()
    {
        var user = User.Create("user@example.com", "hashedPw", "John", "Doe", null);
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto("token", string.Empty, DateTime.UtcNow.AddHours(1)));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("refreshToken");

        var command = new LoginCommand(new LoginRequest("user@example.com", "pass"));
        await _handler.Handle(command, CancellationToken.None);

        _refreshTokenRepoMock.Verify(r => r.Stage(It.IsAny<RefreshToken>()), Times.Once);
    }
}
