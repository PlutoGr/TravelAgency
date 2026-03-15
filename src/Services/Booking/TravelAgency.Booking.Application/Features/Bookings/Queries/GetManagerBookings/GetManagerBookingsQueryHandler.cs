using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetManagerBookings;

public sealed class GetManagerBookingsQueryHandler(
    IBookingRepository bookingRepository,
    IIdentityGrpcClient identityGrpcClient)
    : IRequestHandler<GetManagerBookingsQuery, IReadOnlyList<BookingDto>>
{
    public async Task<IReadOnlyList<BookingDto>> Handle(GetManagerBookingsQuery query, CancellationToken cancellationToken)
    {
        var bookings = await bookingRepository.GetAllForManagersAsync(cancellationToken);

        var result = new List<BookingDto>(bookings.Count);
        var clientCache = new Dictionary<Guid, UserSummary?>();

        foreach (var booking in bookings)
        {
            if (!clientCache.TryGetValue(booking.ClientId, out var summary))
            {
                summary = await identityGrpcClient.GetUserSummaryAsync(booking.ClientId, cancellationToken);
                clientCache[booking.ClientId] = summary;
            }

            var clientName = summary != null ? $"{summary.FirstName} {summary.LastName}".Trim() : null;
            var clientEmail = summary?.Email;
            var clientPhone = (string?)null;

            result.Add(booking.ToDto(clientName, clientEmail, clientPhone));
        }

        return result.AsReadOnly();
    }
}
