using System.Text.Json;
using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;

public sealed class CreateBookingCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    ICatalogGrpcClient catalogGrpcClient,
    IOutboxMessageRepository outboxRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        _ = await catalogGrpcClient.GetTourSnapshotAsync(command.Request.TourId, cancellationToken);

        var booking = TravelAgency.Booking.Domain.Entities.Booking.Create(
            currentUser.UserId,
            command.Request.TourId,
            command.Request.Comment);

        bookingRepository.Stage(booking);

        var payload = JsonSerializer.Serialize(new
        {
            booking.Id,
            booking.ClientId,
            booking.TourId,
            booking.CreatedAt
        });
        outboxRepository.Stage(OutboxMessage.Create("BookingCreated", payload));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}
