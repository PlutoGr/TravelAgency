using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Infrastructure.Grpc;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.Infrastructure.GrpcServices;

public class CatalogGrpcService : CatalogService.CatalogServiceBase
{
    private readonly CatalogDbContext _db;

    public CatalogGrpcService(CatalogDbContext db)
    {
        _db = db;
    }

    public override async Task<TourSnapshotResponse> GetTourSnapshot(
        GetTourSnapshotRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.TourId, out var tourId))
            return new TourSnapshotResponse { Found = false };

        var now = DateTime.UtcNow;
        var tour = await _db.Tours
            .Include(t => t.Prices)
            .FirstOrDefaultAsync(t => t.Id == tourId && t.IsActive, context.CancellationToken);

        if (tour == null)
            return new TourSnapshotResponse { Found = false };

        var activePrice = tour.Prices
            .Where(p => p.ValidFrom <= now && p.ValidTo >= now)
            .OrderBy(p => p.PricePerPerson)
            .FirstOrDefault();

        return new TourSnapshotResponse
        {
            TourId = tour.Id.ToString(),
            Title = tour.Title,
            Country = tour.Country,
            DurationDays = tour.DurationDays,
            PricePerPerson = (double)(activePrice?.PricePerPerson ?? 0),
            Currency = activePrice?.Currency ?? "USD",
            Found = true
        };
    }
}
