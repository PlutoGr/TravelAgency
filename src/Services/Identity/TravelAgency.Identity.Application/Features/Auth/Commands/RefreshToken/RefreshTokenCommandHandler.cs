using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IOptions<JwtSettings> jwtSettings,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AuthTokensDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var oldToken = await refreshTokenRepository.GetByTokenAsync(command.Request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        if (oldToken.IsRevoked)
        {
            // Уже отозванный токен: гасятся все активные сессии. Значение токена в лог не пишется.
            logger.LogWarning(
                "Повторное использование отозванного refresh-токена. UserId: {UserId}, RefreshTokenId: {RefreshTokenId}.",
                oldToken.UserId,
                oldToken.Id);
            await refreshTokenRepository.RevokeAllByUserIdAsync(oldToken.UserId, cancellationToken);
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        if (!oldToken.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await userRepository.GetByIdAsync(oldToken.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        if (user.IsBlocked)
        {
            await refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var accessTokenDto = jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenString = jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            user.Id,
            newRefreshTokenString,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        var rotated = await refreshTokenRepository.TryRotateAsync(
            oldToken.Id,
            newRefreshToken,
            newRefreshTokenString,
            cancellationToken);

        if (!rotated)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        return new AuthTokensDto(
            accessTokenDto.AccessToken,
            newRefreshTokenString,
            accessTokenDto.ExpiresAt,
            newRefreshToken.ExpiresAt);
    }
}
