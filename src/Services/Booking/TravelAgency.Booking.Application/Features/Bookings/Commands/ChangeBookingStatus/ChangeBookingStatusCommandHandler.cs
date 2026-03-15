using System.Text.Json;
using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.ChangeBookingStatus;

public sealed class ChangeBookingStatusCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IOutboxMessageRepository outboxRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeBookingStatusCommand, BookingDto>
{
    public async Task<BookingDto> Handle(ChangeBookingStatusCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.NewStatus == BookingStatus.Confirmed)
            throw new BadRequestException("Use POST /bookings/{id}/confirm to confirm a proposal.");

        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{command.BookingId}' was not found.");

        EnforceAuthorizationPolicy(booking, command.Request.NewStatus);

        var oldStatus = booking.Status;
        booking.TransitionTo(command.Request.NewStatus, currentUser.UserId);

        var payload = JsonSerializer.Serialize(new
        {
            BookingId = booking.Id,
            OldStatus = oldStatus.ToString(),
            NewStatus = command.Request.NewStatus.ToString(),
            ChangedBy = currentUser.UserId
        });
        outboxRepository.Stage(OutboxMessage.Create("BookingStatusChanged", payload));

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
