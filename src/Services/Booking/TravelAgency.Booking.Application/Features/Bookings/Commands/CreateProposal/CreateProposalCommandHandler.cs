using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Domain.ValueObjects;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.CreateProposal;

public sealed class CreateProposalCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    ICatalogGrpcClient catalogGrpcClient,
    IOutboxMessageRepository outboxRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProposalCommand, ProposalDto>
{
    public async Task<ProposalDto> Handle(CreateProposalCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Role != AppRoles.Manager && currentUser.Role != AppRoles.Admin)
            throw new ForbiddenException("Only managers or admins can create proposals.");

        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{command.BookingId}' was not found.");

        var snapshotDto = await catalogGrpcClient.GetTourSnapshotAsync(booking.TourId, cancellationToken);

        var snapshot = new TourSnapshot(
            snapshotDto.TourId,
            snapshotDto.Title,
            snapshotDto.Description,
            snapshotDto.Price,
            snapshotDto.Currency,
            snapshotDto.DurationDays,
            snapshotDto.SnapshotTakenAt);

        booking.AddProposal(currentUser.UserId, snapshot, command.Request.Notes);

        foreach (var msg in booking.OutboxMessages)
            outboxRepository.Stage(msg);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var proposal = booking.Proposals.Last();
        return proposal.ToDto();
    }
}
