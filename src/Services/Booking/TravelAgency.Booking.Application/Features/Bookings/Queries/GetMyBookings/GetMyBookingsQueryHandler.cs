using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetMyBookings;

public sealed class GetMyBookingsQueryHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IIdentityGrpcClient identityGrpcClient)
    : IRequestHandler<GetMyBookingsQuery, IReadOnlyList<BookingDto>>
{
    public async Task<IReadOnlyList<BookingDto>> Handle(GetMyBookingsQuery query, CancellationToken cancellationToken)
    {
        var bookings = await bookingRepository.GetByClientIdAsync(currentUser.UserId, cancellationToken);
        var result = new List<BookingDto>(bookings.Count);

        foreach (var booking in bookings)
        {
            var managerName = await BookingView.ResolveManagerNameAsync(
                booking.AssignedManagerId, identityGrpcClient, cancellationToken);
            result.Add(booking.ToDto(assignedManagerName: managerName));
        }

        return result.AsReadOnly();
    }
}
