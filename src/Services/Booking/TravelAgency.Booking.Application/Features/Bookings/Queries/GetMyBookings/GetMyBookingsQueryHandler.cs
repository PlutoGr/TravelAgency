using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetMyBookings;

public sealed class GetMyBookingsQueryHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository)
    : IRequestHandler<GetMyBookingsQuery, IReadOnlyList<BookingDto>>
{
    public async Task<IReadOnlyList<BookingDto>> Handle(GetMyBookingsQuery query, CancellationToken cancellationToken)
    {
        var bookings = await bookingRepository.GetByClientIdAsync(currentUser.UserId, cancellationToken);
        return bookings.Select(b => b.ToDto()).ToList().AsReadOnly();
    }
}
