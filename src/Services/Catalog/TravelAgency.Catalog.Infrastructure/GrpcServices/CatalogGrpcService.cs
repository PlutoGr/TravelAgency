using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Contracts.Grpc.Catalog;

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

        // Fallback: if no price in current date range, use minimum price from any price
        var priceToUse = activePrice ?? tour.Prices.OrderBy(p => p.PricePerPerson).FirstOrDefault();

        return new TourSnapshotResponse
        {
            TourId = tour.Id.ToString(),
            Title = tour.Title,
            Description = tour.Description,
            Price = (double)(priceToUse?.PricePerPerson ?? 0),
            Currency = priceToUse?.Currency ?? "USD",
            DurationDays = tour.DurationDays,
            SnapshotTakenAt = now.ToString("O"),
            Found = true
        };
    }
}
