using MediatR;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetBookingById;

public record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDto>;
