using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed record InspectPasswordResetCommand(string? Token) : IRequest<AccountOutcome>;

public sealed class InspectPasswordResetCommandHandler(
    IAccountLinkStore store,
    IUserRepository users,
    TimeProvider clock)
    : IRequestHandler<InspectPasswordResetCommand, AccountOutcome>
{
    public async Task<AccountOutcome> Handle(InspectPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var alive = await AliveReset.FindAsync(store, users, clock, command.Token, cancellationToken);
        return new AccountOutcome(alive is null ? "invalid" : "ready");
    }
}

internal static class AliveReset
{
    public static async Task<(Domain.Entities.PasswordResetToken Token, Domain.Entities.User User)?> FindAsync(
        IAccountLinkStore store,
        IUserRepository users,
        TimeProvider clock,
        string? secret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return null;

        var row = await store.FindPasswordResetAsync(LinkSecret.Hash(secret), cancellationToken);
        if (row is null || row.ConsumedAt is not null || row.ExpiresAt <= clock.GetUtcNow().UtcDateTime)
            return null;

        var user = await users.GetByIdAsync(row.UserId, cancellationToken);
        if (user is null || user.IsBlocked)
            return null;

        return (row, user);
    }
}
