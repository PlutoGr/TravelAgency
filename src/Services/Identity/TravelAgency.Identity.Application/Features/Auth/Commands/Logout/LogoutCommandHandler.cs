using MediatR;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var token = await refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);

        if (token is null)
            return;

        if (token.UserId != command.UserId)
            throw new UnauthorizedException("You are not authorized to revoke this token.");

        if (token.IsActive)
        {
            token.Revoke();
            await refreshTokenRepository.UpdateAsync(token, cancellationToken);
        }
    }
}
