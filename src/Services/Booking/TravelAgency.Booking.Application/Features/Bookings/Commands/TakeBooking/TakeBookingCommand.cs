using MediatR;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.TakeBooking;

public record TakeBookingCommand(Guid BookingId, TakeBookingRequest Request) : IRequest<BookingDto>;
