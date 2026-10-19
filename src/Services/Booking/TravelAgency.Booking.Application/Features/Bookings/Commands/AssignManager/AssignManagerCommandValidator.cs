using FluentValidation;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.AssignManager;

public sealed class AssignManagerCommandValidator : AbstractValidator<AssignManagerCommand>
{
    public AssignManagerCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId must not be empty.");

        RuleFor(x => x.Request)
            .NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.ManagerId)
                .NotEmpty().WithMessage("ManagerId must not be empty.");

            RuleFor(x => x.Request.ExpectedManagerId)
                .Must(id => id is null || id != Guid.Empty)
                .WithMessage("ExpectedManagerId must not be empty.");

            RuleFor(x => x.Request.Comment)
                .MaximumLength(500);
        });
    }
}
