using FluentValidation;

namespace TravelAgency.Identity.Application.Features.Auth.Account;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Email)
            .Must(EmailAddressRules.IsValid)
            .WithErrorCode(EmailAddressRules.InvalidCode);
    }
}
