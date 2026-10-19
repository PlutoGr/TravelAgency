using MediatR;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetManagerBookings;

public sealed record GetManagerBookingsQuery(string? Assigned = null) : IRequest<IReadOnlyList<BookingDto>>;
