using MediatR;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed record ConfirmEmailChangeCommand(string? Token, string? RefreshToken) : IRequest<AccountOutcome>;

public sealed class ConfirmEmailChangeCommandHandler(
    IAccountLinkStore store,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    ICurrentUserService currentUser,
    TimeProvider clock,
    ILogger<ConfirmEmailChangeCommandHandler> logger)
    : IRequestHandler<ConfirmEmailChangeCommand, AccountOutcome>
{
    public async Task<AccountOutcome> Handle(ConfirmEmailChangeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return new AccountOutcome("invalid");

        var row = await store.FindEmailChangeByConfirmAsync(LinkSecret.Hash(command.Token), cancellationToken);
        if (row is null || row.ConsumedAt is not null || row.ExpiresAt <= clock.GetUtcNow().UtcDateTime)
            return new AccountOutcome("invalid");

        var user = await users.GetByIdAsync(row.UserId, cancellationToken);
        if (user is null || user.IsBlocked)
            return new AccountOutcome("invalid");

        var session = AccountSessionKind.From(
            currentUser.IsAuthenticated ? currentUser.UserId : null,
            user.Id);
        Guid? keep = null;
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            var refresh = await refreshTokens.GetByTokenAsync(command.RefreshToken, cancellationToken);
            if (refresh is { IsActive: true } && refresh.UserId == user.Id)
                keep = refresh.Id;
        }

        var result = await store.TryConsumeEmailConfirmationAsync(
            row.Id,
            user,
            row.NewEmail,
            keep,
            cancellationToken);
        if (result == EmailChangeConsumption.Lost)
            return new AccountOutcome("invalid");
        if (result == EmailChangeConsumption.Taken)
            return new AccountOutcome("taken");

        logger.LogInformation("Адрес подтверждён {UserId} {EmailChangeId}", user.Id, row.Id);
        return new AccountOutcome("changed", session);
    }
}
