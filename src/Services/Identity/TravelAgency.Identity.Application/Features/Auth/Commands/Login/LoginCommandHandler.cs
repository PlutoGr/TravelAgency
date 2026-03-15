using MediatR;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher,
    ILockoutService lockoutService,
    IUnitOfWork unitOfWork,
    IOptions<JwtSettings> jwtSettings)
    : IRequestHandler<LoginCommand, AuthTokensDto>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AuthTokensDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var email = request.Email.Trim().ToLowerInvariant();

        if (lockoutService.IsLockedOut(email))
            throw new UnauthorizedException("Invalid credentials.");

        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            lockoutService.RecordFailedAttempt(email);
            throw new UnauthorizedException("Invalid credentials.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            lockoutService.RecordFailedAttempt(email);
            throw new UnauthorizedException("Invalid credentials.");
        }

        lockoutService.ResetFailedAttempts(email);

        var accessTokenDto = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenString = jwtTokenService.GenerateRefreshToken();

        var refreshToken = TravelAgency.Identity.Domain.Entities.RefreshToken.Create(
            user.Id,
            refreshTokenString,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        refreshTokenRepository.Stage(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(accessTokenDto.AccessToken, refreshTokenString, accessTokenDto.ExpiresAt);
    }
}
