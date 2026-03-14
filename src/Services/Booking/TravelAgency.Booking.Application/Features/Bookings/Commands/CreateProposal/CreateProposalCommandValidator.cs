using FluentValidation;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateProposal;

public sealed class CreateProposalCommandValidator : AbstractValidator<CreateProposalCommand>
{
    public CreateProposalCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId must not be empty.");
    }
}
