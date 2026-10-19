using MediatR;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.AssignManager;

public record AssignManagerCommand(Guid BookingId, AssignManagerRequest Request) : IRequest<BookingDto>;
