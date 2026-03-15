using Microsoft.Extensions.Configuration;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Contracts.Grpc.Catalog;

namespace TravelAgency.Booking.Infrastructure.GrpcClients;

public class CatalogGrpcClient : ICatalogGrpcClient
{
    private readonly CatalogService.CatalogServiceClient _client;
    private readonly IConfiguration _configuration;

    public CatalogGrpcClient(CatalogService.CatalogServiceClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<BookingTourSnapshotDto> GetTourSnapshotAsync(Guid tourId, CancellationToken ct = default)
    {
        var request = new GetTourSnapshotRequest { TourId = tourId.ToString() };
        var callOptions = CreateCallOptions(ct);
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

    private Grpc.Core.CallOptions CreateCallOptions(CancellationToken ct = default)
    {
        var token = _configuration["GrpcSettings:InternalServiceToken"];
        if (string.IsNullOrEmpty(token))
            return new Grpc.Core.CallOptions(cancellationToken: ct);

        var metadata = new Grpc.Core.Metadata
        {
            { "x-internal-auth", token }
        };
        return new Grpc.Core.CallOptions(metadata, deadline: null, ct);
    }
}
