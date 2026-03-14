using FluentValidation;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.Request.TourId)
            .NotEmpty().WithMessage("TourId must not be empty.");
    }
}
