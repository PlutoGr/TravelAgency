using System.Text.Json;
using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Features.Bookings;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;

public sealed class CreateBookingCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    ICatalogGrpcClient catalogGrpcClient,
    IOutboxMessageRepository outboxRepository,
    IUnitOfWork unitOfWork,
    IIdentityGrpcClient identityGrpcClient)
    : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        _ = await catalogGrpcClient.GetTourSnapshotAsync(command.Request.TourId, cancellationToken);

        var preferredManagerId = await bookingRepository.GetPreferredManagerIdAsync(
            currentUser.UserId, cancellationToken);

        var booking = Domain.Entities.Booking.Create(
            currentUser.UserId,
            command.Request.TourId,
            command.Request.Comment,
            preferredManagerId);

        bookingRepository.Stage(booking);

        var payload = JsonSerializer.Serialize(new
        {
            booking.Id,
            booking.ClientId,
            booking.TourId,
            booking.CreatedAt
        });
        outboxRepository.Stage(OutboxMessage.Create("BookingCreated", payload));

        if (booking.AssignedManagerId is Guid managerId)
        {
            outboxRepository.Stage(BookingManagerAssigned.CreateMessage(
                booking.Id, null, managerId, currentUser.UserId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var managerName = await BookingView.ResolveManagerNameAsync(
            booking.AssignedManagerId, identityGrpcClient, cancellationToken);

        return booking.ToDto(assignedManagerName: managerName);
    }
}
