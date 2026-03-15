using MediatR;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.ConfirmProposal;

public record ConfirmProposalCommand(Guid BookingId, ConfirmProposalRequest Request) : IRequest<BookingDto>;
