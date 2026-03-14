using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;

public sealed class CreateBookingCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = Domain.Entities.Booking.Create(
            currentUser.UserId,
            command.Request.TourId,
            command.Request.Comment);

        bookingRepository.Stage(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}
