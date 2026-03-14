using MediatR;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.ChangeBookingStatus;

public record ChangeBookingStatusCommand(Guid BookingId, ChangeBookingStatusRequest Request) : IRequest<BookingDto>;
