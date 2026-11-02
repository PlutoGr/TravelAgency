using MediatR;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed record CancelEmailChangeCommand(string? Token) : IRequest<AccountOutcome>;

public sealed class CancelEmailChangeCommandHandler(
    IAccountLinkStore store,
    IUserRepository users,
    TimeProvider clock,
    ILogger<CancelEmailChangeCommandHandler> logger)
    : IRequestHandler<CancelEmailChangeCommand, AccountOutcome>
{
    public async Task<AccountOutcome> Handle(CancelEmailChangeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return new AccountOutcome("invalid");

        var row = await store.FindEmailChangeByCancelAsync(LinkSecret.Hash(command.Token), cancellationToken);
        if (row is null || row.ConsumedAt is not null || row.ExpiresAt <= clock.GetUtcNow().UtcDateTime)
            return new AccountOutcome("invalid");

        var user = await users.GetByIdAsync(row.UserId, cancellationToken);
        if (user is null || user.IsBlocked)
            return new AccountOutcome("invalid");

        var consumed = await store.TryConsumeEmailCancellationAsync(row.Id, cancellationToken);
        if (!consumed)
            return new AccountOutcome("invalid");

        logger.LogInformation("Смена адреса отменена {UserId} {EmailChangeId}", user.Id, row.Id);
        return new AccountOutcome("cancelled");
    }
}
