using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.ConfirmProposal;

public sealed class ConfirmProposalCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmProposalCommand, BookingDto>
{
    public async Task<BookingDto> Handle(ConfirmProposalCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{command.BookingId}' was not found.");

        EnforceAuthorizationPolicy(booking);

        booking.ConfirmProposal(command.Request.ProposalId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }

    private void EnforceAuthorizationPolicy(Domain.Entities.Booking booking)
    {
        if (currentUser.Role == AppRoles.Client)
        {
            if (booking.ClientId != currentUser.UserId)
                throw new ForbiddenException("Clients can only confirm their own bookings.");
        }
        else if (currentUser.Role != AppRoles.Manager && currentUser.Role != AppRoles.Admin)
        {
            throw new ForbiddenException("Only clients, managers, or admins can confirm proposals.");
        }
    }
}
