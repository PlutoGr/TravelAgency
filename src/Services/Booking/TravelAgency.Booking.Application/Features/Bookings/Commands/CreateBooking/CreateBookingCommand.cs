using MediatR;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;

public record CreateBookingCommand(CreateBookingRequest Request) : IRequest<BookingDto>;
