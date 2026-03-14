using MediatR;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetMyBookings;

public record GetMyBookingsQuery : IRequest<IReadOnlyList<BookingDto>>;
