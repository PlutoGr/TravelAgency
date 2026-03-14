using MediatR;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IOptions<JwtSettings> jwtSettings)
    : IRequestHandler<RegisterCommand, AuthTokensDto>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AuthTokensDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (await userRepository.ExistsAsync(request.Email, cancellationToken))
            throw new ConflictException("A user with this email already exists.");

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.Email,
            passwordHash,
            request.FirstName,
            request.LastName,
            request.Phone);

        var accessTokenDto = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenString = jwtTokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenString,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        userRepository.Stage(user);
        refreshTokenRepository.Stage(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(accessTokenDto.AccessToken, refreshTokenString, accessTokenDto.ExpiresAt);
    }
}
