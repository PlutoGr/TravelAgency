using MediatR;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetManagerBookings;

public sealed record GetManagerBookingsQuery : IRequest<IReadOnlyList<BookingDto>>;
