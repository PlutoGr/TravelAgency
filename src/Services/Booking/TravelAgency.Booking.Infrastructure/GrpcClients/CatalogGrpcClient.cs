using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Contracts.Grpc.Catalog;

namespace TravelAgency.Booking.Infrastructure.GrpcClients;

public class CatalogGrpcClient : ICatalogGrpcClient
{
    private readonly CatalogService.CatalogServiceClient _client;

    public CatalogGrpcClient(CatalogService.CatalogServiceClient client)
    {
        _client = client;
    }

    public async Task<TourSnapshotDto> GetTourSnapshotAsync(Guid tourId, CancellationToken ct = default)
    {
        var request = new GetTourSnapshotRequest { TourId = tourId.ToString() };
        var response = await _client.GetTourSnapshotAsync(request, cancellationToken: ct);

        if (!response.Found)
            throw new NotFoundException($"Tour '{tourId}' was not found in catalog.");

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
