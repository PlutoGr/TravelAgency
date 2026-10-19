using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetManagerBookings;

public sealed class GetManagerBookingsQueryHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IIdentityGrpcClient identityGrpcClient)
    : IRequestHandler<GetManagerBookingsQuery, IReadOnlyList<BookingDto>>
{
    public async Task<IReadOnlyList<BookingDto>> Handle(GetManagerBookingsQuery query, CancellationToken cancellationToken)
    {
        var assigned = string.IsNullOrWhiteSpace(query.Assigned)
            ? "all"
            : query.Assigned.Trim().ToLowerInvariant();

        if (assigned is not ("me" or "queue" or "all"))
            throw new BadRequestException("assigned must be me, queue, or all.");

        var bookings = await bookingRepository.GetAllForManagersAsync(cancellationToken);
        IReadOnlyList<Domain.Entities.Booking> filtered = assigned switch
        {
            "me" => bookings.Where(b => b.AssignedManagerId == currentUser.UserId).ToList(),
            "queue" => bookings.Where(b => b.AssignedManagerId is null).ToList(),
            _ => bookings
        };

        return await BookingView.ForStaffListAsync(filtered, identityGrpcClient, cancellationToken);
    }
}
