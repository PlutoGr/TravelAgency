using FluentValidation;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.ChangeBookingStatus;

public sealed class ChangeBookingStatusCommandValidator : AbstractValidator<ChangeBookingStatusCommand>
{
    public ChangeBookingStatusCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId must not be empty.");
    }
}
