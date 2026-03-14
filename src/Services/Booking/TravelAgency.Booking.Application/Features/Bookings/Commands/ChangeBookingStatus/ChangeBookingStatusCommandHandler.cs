using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.ChangeBookingStatus;

public sealed class ChangeBookingStatusCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeBookingStatusCommand, BookingDto>
{
    public async Task<BookingDto> Handle(ChangeBookingStatusCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{command.BookingId}' was not found.");

        EnforceAuthorizationPolicy(booking, command.Request.NewStatus);

        booking.TransitionTo(command.Request.NewStatus, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }

    private void EnforceAuthorizationPolicy(Domain.Entities.Booking booking, BookingStatus newStatus)
    {
        if (currentUser.Role == AppRoles.Client)
        {
            if (booking.ClientId != currentUser.UserId)
                throw new ForbiddenException("Clients can only modify their own bookings.");

            if (newStatus != BookingStatus.Cancelled)
                throw new ForbiddenException("Clients can only cancel their bookings.");
        }
    }
}
