using MediatR;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed record CompletePasswordResetCommand(string? Token, string? Password, string? PasswordConfirm)
    : IRequest<AccountOutcome>;

public sealed class CompletePasswordResetCommandHandler(
    IAccountLinkStore store,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ILockoutService lockout,
    IAccountMailSender mail,
    AccountMailComposer composer,
    AccountSessionPublisher sessions,
    ICurrentUserService currentUser,
    TimeProvider clock,
    ILogger<CompletePasswordResetCommandHandler> logger)
    : IRequestHandler<CompletePasswordResetCommand, AccountOutcome>
{
    public async Task<AccountOutcome> Handle(CompletePasswordResetCommand command, CancellationToken cancellationToken)
    {
        var alive = await AliveReset.FindAsync(store, users, clock, command.Token, cancellationToken);
        if (alive is null)
            return new AccountOutcome("invalid");

        var (row, user) = alive.Value;
        if (!string.Equals(command.Password, command.PasswordConfirm, StringComparison.Ordinal))
            throw new CodedBadRequestException(PasswordRules.MismatchCode);
        if (!PasswordRules.IsSatisfied(command.Password))
            throw new CodedBadRequestException(PasswordRules.Code);

        var session = AccountSessionKind.From(
            currentUser.IsAuthenticated ? currentUser.UserId : null,
            user.Id);
        var consumed = await store.TryConsumePasswordResetAsync(
            row.Id,
            user,
            passwordHasher.Hash(command.Password!),
            cancellationToken);
        if (!consumed)
            return new AccountOutcome("invalid");

        lockout.ResetFailedAttempts(user.Email);
        await sessions.PublishAsync(user.Id, user.SessionVersion, blocked: false, cancellationToken);
        logger.LogInformation("Пароль задан по ссылке {UserId} {PasswordResetId}", user.Id, row.Id);

        if (composer.CanLink)
        {
            var sent = await mail.SendAsync(composer.PasswordChanged(user.Email), cancellationToken);
            if (!sent)
                logger.LogWarning("Письмо о смене пароля не принято {UserId} {PasswordResetId}", user.Id, row.Id);
        }

        return new AccountOutcome("changed", session);
    }
}
