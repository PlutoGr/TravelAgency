using FluentValidation;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.ConfirmProposal;

public sealed class ConfirmProposalCommandValidator : AbstractValidator<ConfirmProposalCommand>
{
    public ConfirmProposalCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId must not be empty.");

        RuleFor(x => x.Request)
            .NotNull().WithMessage("Request body is required.");

        RuleFor(x => x.Request!.ProposalId)
            .NotEmpty().WithMessage("ProposalId must not be empty.")
            .When(x => x.Request != null);
    }
}
