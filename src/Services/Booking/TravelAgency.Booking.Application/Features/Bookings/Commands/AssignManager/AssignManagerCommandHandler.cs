using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Features.Bookings.Commands.AssignManager;

public sealed class AssignManagerCommandHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IOutboxMessageRepository outboxRepository,
    IUnitOfWork unitOfWork,
    IIdentityGrpcClient identityGrpcClient)
    : IRequestHandler<AssignManagerCommand, BookingDto>
{
    public async Task<BookingDto> Handle(AssignManagerCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not (AppRoles.Manager or AppRoles.Admin))
            throw new ForbiddenException("Only managers or admins can assign a manager.");

        var target = await identityGrpcClient.GetUserSummaryAsync(command.Request.ManagerId, cancellationToken);
        if (target is null || target.Role != AppRoles.Manager)
            throw new BadRequestException("Target user is not a manager.");

        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{command.BookingId}' was not found.");

        var previousManagerId = booking.AssignedManagerId;
        var changed = booking.AssignManager(
            command.Request.ManagerId,
            currentUser.UserId,
            command.Request.ExpectedManagerId,
            command.Request.Comment);

        if (changed)
        {
            outboxRepository.Stage(BookingManagerAssigned.CreateMessage(
                booking.Id,
                previousManagerId,
                command.Request.ManagerId,
                currentUser.UserId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BookingView.ForViewerAsync(
            booking, currentUser.Role, identityGrpcClient, cancellationToken);
    }
}
