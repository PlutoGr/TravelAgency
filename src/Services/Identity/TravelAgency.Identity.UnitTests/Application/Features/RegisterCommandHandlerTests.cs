using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Auth.Commands.Register;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RegisterCommandHandler _handler;

    private readonly JwtSettings _jwtSettings = new()
    {
        RefreshTokenExpirationDays = 7
    };

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(
            _userRepoMock.Object,
            _refreshTokenRepoMock.Object,
            _jwtServiceMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            Options.Create(_jwtSettings));
    }

    [Fact]
    public async Task Handle_WithNewEmail_ReturnsAuthTokensDto()
    {
        var accessToken = "access.token";
        var refreshToken = "refresh-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _userRepoMock.Setup(r => r.ExistsAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash("Password1")).Returns("hashed");
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto(accessToken, string.Empty, expiresAt));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns(refreshToken);

        var request = new RegisterRequest("new@example.com", "Password1", "John", "Doe", null);
        var result = await _handler.Handle(new RegisterCommand(request), CancellationToken.None);

        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsConflictException()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new RegisterRequest("existing@example.com", "Password1", "John", "Doe", null);

        var act = async () => await _handler.Handle(new RegisterCommand(request), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WithNewEmail_HashesPassword()
    {
        _userRepoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash("PlainPass")).Returns("hashedPass");
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto("t", string.Empty, DateTime.UtcNow.AddHours(1)));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("rt");

        var request = new RegisterRequest("u@example.com", "PlainPass", "J", "D", null);
        await _handler.Handle(new RegisterCommand(request), CancellationToken.None);

        _passwordHasherMock.Verify(h => h.Hash("PlainPass"), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewEmail_StagesUserToRepository()
    {
        _userRepoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto("t", string.Empty, DateTime.UtcNow.AddHours(1)));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("rt");

        var request = new RegisterRequest("u@example.com", "Pass1", "J", "D", null);
        await _handler.Handle(new RegisterCommand(request), CancellationToken.None);

        _userRepoMock.Verify(r => r.Stage(It.IsAny<User>()), Times.Once);
    }

    // FIX-010: UnitOfWork pattern — a single SaveChangesAsync call commits user + refresh token atomically

    [Fact]
    public async Task Handle_WhenRegistrationSucceeds_CallsUnitOfWorkSaveChangesOnce()
    {
        _userRepoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto("t", string.Empty, DateTime.UtcNow.AddHours(1)));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("rt");
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var request = new RegisterRequest("u@example.com", "Pass1", "J", "D", null);
        await _handler.Handle(new RegisterCommand(request), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkThrows_PropagatesException()
    {
        _userRepoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto("t", string.Empty, DateTime.UtcNow.AddHours(1)));
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("rt");
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        var request = new RegisterRequest("u@example.com", "Pass1", "J", "D", null);
        var act = async () => await _handler.Handle(new RegisterCommand(request), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB failure");
    }
}
