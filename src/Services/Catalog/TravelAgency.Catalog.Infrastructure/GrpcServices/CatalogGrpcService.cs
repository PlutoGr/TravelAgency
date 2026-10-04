using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Domain.Enums;
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
            .Include(t => t.Offers)
            .FirstOrDefaultAsync(t => t.Id == tourId && t.Status == TourStatus.Published, context.CancellationToken);

        if (tour == null)
            return new TourSnapshotResponse { Found = false };

        var activePrice = tour.Offers
            .Where(p => p.ValidFrom <= now && p.ValidTo >= now)
            .OrderBy(p => p.PricePerPerson)
            .FirstOrDefault();

        // Fallback: if no price in current date range, use minimum price from any offer.
        // Поля снимка не переименовываются: контракт catalog.proto с Booking остаётся прежним.
        var priceToUse = activePrice ?? tour.Offers.OrderBy(p => p.PricePerPerson).FirstOrDefault();

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
