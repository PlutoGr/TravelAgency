using FluentValidation;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.TakeBooking;

public sealed class TakeBookingCommandValidator : AbstractValidator<TakeBookingCommand>
{
    public TakeBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId must not be empty.");

        RuleFor(x => x.Request)
            .NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.ExpectedManagerId)
                .Must(id => id is null || id != Guid.Empty)
                .WithMessage("ExpectedManagerId must not be empty.");
        });
    }
}
