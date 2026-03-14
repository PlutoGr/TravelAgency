using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.Infrastructure.Repositories;

public class TourRepository : ITourRepository
{
    private readonly CatalogDbContext _db;

    public TourRepository(CatalogDbContext db)
    {
        _db = db;
    }

    public Task<Tour?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Tours.Include(t => t.Prices).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<TourSummaryDto>> GetPagedAsync(ToursFilterDto filter, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var query = _db.Tours.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Country))
            query = query.Where(t => t.Country.ToLower().Contains(filter.Country.ToLower()));

        if (filter.TourType.HasValue)
            query = query.Where(t => t.TourType == filter.TourType.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(t => t.IsActive == filter.IsActive.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(t => t.Prices.Any(p =>
                p.ValidFrom <= now && p.ValidTo >= now &&
                p.PricePerPerson >= filter.MinPrice.Value));

        if (filter.MaxPrice.HasValue)
            query = query.Where(t => t.Prices.Any(p =>
                p.ValidFrom <= now && p.ValidTo >= now &&
                p.PricePerPerson <= filter.MaxPrice.Value));

        if (filter.DateFrom.HasValue)
            query = query.Where(t => t.Prices.Any(p => p.ValidFrom >= filter.DateFrom.Value));

        if (filter.DateTo.HasValue)
            query = query.Where(t => t.Prices.Any(p => p.ValidTo <= filter.DateTo.Value));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TourSummaryDto(
                t.Id,
                t.Title,
                t.Country,
                t.TourType,
                t.DurationDays,
                t.ImageUrl,
                t.Prices
                    .Where(p => p.ValidFrom <= now && p.ValidTo >= now)
                    .Select(p => (decimal?)p.PricePerPerson)
                    .Min(),
                t.Prices
                    .Where(p => p.ValidFrom <= now && p.ValidTo >= now)
                    .OrderBy(p => p.PricePerPerson)
                    .Select(p => p.Currency)
                    .FirstOrDefault(),
                t.IsActive))
            .ToListAsync(ct);

        return new PagedResult<TourSummaryDto>(items, totalCount, filter.Page, filter.PageSize);
    }

    public Task AddAsync(Tour tour, CancellationToken ct = default)
        => _db.Tours.AddAsync(tour, ct).AsTask();

    public void Update(Tour tour)
        => _db.Tours.Update(tour);

    public Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default)
        => _db.Tours.AnyAsync(t => t.Title == title, ct);
}
