using MediatR;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateProposal;

public record CreateProposalCommand(Guid BookingId, CreateProposalRequest Request) : IRequest<ProposalDto>;
