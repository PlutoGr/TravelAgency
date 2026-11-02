using MediatR;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed record ChangePasswordCommand(string? CurrentPassword, string? NewPassword, string? PasswordConfirm)
    : IRequest<AuthTokensDto>;

public sealed class ChangePasswordCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt,
    IAccountLinkStore store,
    AccountSessionPublisher sessions,
    IOptions<JwtSettings> jwtSettings)
    : IRequestHandler<ChangePasswordCommand, AuthTokensDto>
{
    public const string CurrentPasswordInvalid = "CurrentPasswordInvalid";

    public async Task<AuthTokensDto> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("User is not authenticated.");

        if (string.IsNullOrEmpty(command.CurrentPassword)
            || !passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            throw new CodedBadRequestException(CurrentPasswordInvalid);

        if (!PasswordRules.IsSatisfied(command.NewPassword))
            throw new CodedBadRequestException(PasswordRules.Code);
        if (!string.Equals(command.NewPassword, command.PasswordConfirm, StringComparison.Ordinal))
            throw new CodedBadRequestException(PasswordRules.MismatchCode);

        AuthTokensDto? issued = null;
        await store.ApplySettingsPasswordAsync(
            user,
            passwordHasher.Hash(command.NewPassword!),
            updated =>
            {
                var access = jwt.GenerateAccessToken(updated);
                var refresh = jwt.GenerateRefreshToken();
                var expires = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays);
                var token = RefreshToken.Create(updated.Id, refresh, expires);
                issued = new AuthTokensDto(access.AccessToken, refresh, access.ExpiresAt, token.ExpiresAt);
                return token;
            },
            cancellationToken);

        await sessions.PublishAsync(user.Id, user.SessionVersion, blocked: false, cancellationToken);
        return issued ?? throw new InvalidOperationException("Password change did not issue a session.");
    }
}
