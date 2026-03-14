using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Abstractions;

public interface ICatalogGrpcClient
{
    Task<TourSnapshotDto> GetTourSnapshotAsync(Guid tourId, CancellationToken ct = default);
}
