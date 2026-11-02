using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Auth;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed record RequestPasswordResetCommand(string Email) : IRequest<AccountMessage>;

public sealed class RequestPasswordResetCommandHandler(
    IUserRepository users,
    IAccountLinkStore store,
    IAccountMailSender mail,
    IAccountMailQuota quota,
    IUnitOfWork unitOfWork,
    AccountMailComposer composer,
    IOptions<AccountLinkSettings> links,
    TimeProvider clock,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : IRequestHandler<RequestPasswordResetCommand, AccountMessage>
{
    public async Task<AccountMessage> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddressRules.Normalize(command.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);

        var canSend = user is { IsBlocked: false } && composer.CanLink;
        var slot = AccountMailSlot.Exhausted;
        if (canSend)
            slot = await quota.TryReserveAsync(IAccountMailQuota.PasswordReset, email, cancellationToken);

        if (canSend && slot == AccountMailSlot.Reserved)
        {
            var secret = LinkSecret.Create();
            var accepted = await mail.SendAsync(composer.PasswordReset(email, secret), cancellationToken);
            if (!accepted)
            {
                await quota.ReleaseAsync(IAccountMailQuota.PasswordReset, email, cancellationToken);
            }
            else
            {
                var created = PasswordResetToken.Create(
                    user!.Id,
                    secret,
                    clock.GetUtcNow().UtcDateTime.Add(links.Value.PasswordResetLifetime));
                if (await store.ReplacePasswordResetAsync(user.Id, created, cancellationToken))
                {
                    logger.LogInformation(
                        "Ссылка восстановления {UserId} {PasswordResetId}",
                        user.Id,
                        created.Id);
                }
            }
        }
        else
        {
            await mail.SendAsync(AccountMail.Decline(email), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AccountMessage(AccountLinkMessages.PasswordResetRequested);
    }
}
