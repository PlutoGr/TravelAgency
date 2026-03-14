using FluentValidation;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Request.FirstName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.Request.FirstName is not null);

        RuleFor(x => x.Request.LastName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.Request.LastName is not null);

        RuleFor(x => x.Request.Phone)
            .MaximumLength(20)
            .Matches(@"^\+?[\d\s\-()]+$").WithMessage("Phone number format is invalid.")
            .When(x => !string.IsNullOrEmpty(x.Request.Phone));
    }
}
