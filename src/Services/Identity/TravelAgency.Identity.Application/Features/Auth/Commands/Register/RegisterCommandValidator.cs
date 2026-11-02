using FluentValidation;
using TravelAgency.Identity.Application.Features.Auth;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Request.Password)
            .Must(PasswordRules.IsSatisfied)
            .WithErrorCode(PasswordRules.Code);

        RuleFor(x => x.Request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Request.Phone)
            .MaximumLength(20)
            .Matches(@"^\+?[\d\s\-()]+$").WithMessage("Phone number format is invalid.")
            .When(x => !string.IsNullOrEmpty(x.Request.Phone));
    }
}
