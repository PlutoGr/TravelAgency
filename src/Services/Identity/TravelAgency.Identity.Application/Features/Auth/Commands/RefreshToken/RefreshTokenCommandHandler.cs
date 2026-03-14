using MediatR;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
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
    IUnitOfWork unitOfWork,
    IOptions<JwtSettings> jwtSettings)
    : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AuthTokensDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var oldToken = await refreshTokenRepository.GetByTokenAsync(command.Request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        if (!oldToken.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await userRepository.GetByIdAsync(oldToken.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var accessTokenDto = jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenString = jwtTokenService.GenerateRefreshToken();

        oldToken.Revoke(newRefreshTokenString);

        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            user.Id,
            newRefreshTokenString,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        refreshTokenRepository.Stage(newRefreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(accessTokenDto.AccessToken, newRefreshTokenString, accessTokenDto.ExpiresAt);
    }
}
