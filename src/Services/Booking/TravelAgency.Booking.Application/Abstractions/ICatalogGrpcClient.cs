using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Abstractions;

public interface ICatalogGrpcClient
{
    Task<BookingTourSnapshotDto> GetTourSnapshotAsync(Guid tourId, CancellationToken ct = default);
}
