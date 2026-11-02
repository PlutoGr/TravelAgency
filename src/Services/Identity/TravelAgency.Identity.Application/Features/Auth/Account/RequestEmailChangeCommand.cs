using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Auth;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed record RequestEmailChangeCommand(string? Email, string? CurrentPassword) : IRequest<AccountMessage>;

public sealed class RequestEmailChangeCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccountLinkStore store,
    IAccountMailSender mail,
    IAccountMailQuota quota,
    AccountMailComposer composer,
    IOptions<AccountLinkSettings> links,
    TimeProvider clock,
    ILogger<RequestEmailChangeCommandHandler> logger)
    : IRequestHandler<RequestEmailChangeCommand, AccountMessage>
{
    public async Task<AccountMessage> Handle(RequestEmailChangeCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("User is not authenticated.");

        if (user.IsBlocked)
            return Neutral();

        if (!EmailAddressRules.IsValid(command.Email))
            throw new CodedBadRequestException(EmailAddressRules.InvalidCode);

        var email = EmailAddressRules.Normalize(command.Email!);
        if (string.Equals(email, user.Email, StringComparison.Ordinal))
            throw new CodedBadRequestException(EmailAddressRules.UnchangedCode);

        if (string.IsNullOrEmpty(command.CurrentPassword)
            || !passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            throw new CodedBadRequestException(ChangePasswordCommandHandler.CurrentPasswordInvalid);

        var owner = await users.GetByEmailAsync(email, cancellationToken);
        var taken = owner is not null && owner.Id != user.Id;
        if (!composer.CanLink)
            return Neutral();

        var slot = await quota.TryReserveAsync(IAccountMailQuota.EmailChange, email, cancellationToken);
        if (slot != AccountMailSlot.Reserved)
            return Neutral();

        var kept = false;
        if (taken)
        {
            kept = await mail.SendAsync(composer.EmailTaken(email), cancellationToken);
            if (kept)
            {
                await store.ReplaceEmailChangeAsync(user.Id, created: null, cancellationToken);
                logger.LogInformation("Письмо о занятом адресе {UserId}", user.Id);
            }
        }
        else
        {
            var confirmSecret = LinkSecret.Create();
            var cancelSecret = LinkSecret.Create();
            var notice = await mail.SendAsync(composer.EmailChangeNotice(user.Email, email, cancelSecret), cancellationToken);
            var confirm = notice
                && await mail.SendAsync(composer.ConfirmEmail(email, confirmSecret), cancellationToken);
            kept = notice && confirm;
            if (kept)
            {
                var created = EmailChangeRequest.Create(
                    user.Id,
                    email,
                    confirmSecret,
                    cancelSecret,
                    clock.GetUtcNow().UtcDateTime.Add(links.Value.EmailChangeLifetime));
                if (await store.ReplaceEmailChangeAsync(user.Id, created, cancellationToken))
                    logger.LogInformation("Запрошена смена адреса {UserId} {EmailChangeId}", user.Id, created.Id);
            }
        }

        if (!kept)
            await quota.ReleaseAsync(IAccountMailQuota.EmailChange, email, cancellationToken);

        return Neutral();
    }

    private static AccountMessage Neutral() => new(AccountLinkMessages.EmailChangeRequested);
}
