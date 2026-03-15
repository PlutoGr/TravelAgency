using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Contracts.Grpc.Catalog;
using TravelAgency.Shared.Infrastructure.GrpcServices;

namespace TravelAgency.Booking.Infrastructure.GrpcClients;

public class CatalogGrpcClient : ICatalogGrpcClient
{
    private readonly CatalogService.CatalogServiceClient _client;
    private readonly IGrpcAuthCallOptionsFactory _callOptionsFactory;

    public CatalogGrpcClient(CatalogService.CatalogServiceClient client, IGrpcAuthCallOptionsFactory callOptionsFactory)
    {
        _client = client;
        _callOptionsFactory = callOptionsFactory;
    }

    public async Task<BookingTourSnapshotDto> GetTourSnapshotAsync(Guid tourId, CancellationToken ct = default)
    {
        var request = new GetTourSnapshotRequest { TourId = tourId.ToString() };
        var callOptions = _callOptionsFactory.Create(ct);
        var response = await _client.GetTourSnapshotAsync(request, callOptions);

        if (!response.Found)
            throw new NotFoundException($"Tour '{tourId}' was not found in catalog.");

        return new BookingTourSnapshotDto(
            TourId: Guid.Parse(response.TourId),
            Title: response.Title,
            Description: response.Description,
            Price: (decimal)response.Price,
            Currency: response.Currency,
            DurationDays: response.DurationDays,
            SnapshotTakenAt: DateTime.Parse(response.SnapshotTakenAt));
    }
}
