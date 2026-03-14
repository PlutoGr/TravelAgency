using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Infrastructure.GrpcClients;

public class CatalogGrpcClient : ICatalogGrpcClient
{
    private readonly CatalogGrpc.CatalogGrpcClient _client;

    public CatalogGrpcClient(CatalogGrpc.CatalogGrpcClient client)
    {
        _client = client;
    }

    public async Task<TourSnapshotDto> GetTourSnapshotAsync(Guid tourId, CancellationToken ct = default)
    {
        var request = new GetTourSnapshotRequest { TourId = tourId.ToString() };
        var response = await _client.GetTourSnapshotAsync(request, cancellationToken: ct);

        return new TourSnapshotDto(
            TourId: Guid.Parse(response.TourId),
            Title: response.Title,
            Description: response.Description,
            Price: (decimal)response.Price,
            Currency: response.Currency,
            DurationDays: response.DurationDays,
            SnapshotTakenAt: DateTime.Parse(response.SnapshotTakenAt));
    }
}
