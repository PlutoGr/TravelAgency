using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Interfaces;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.Infrastructure.Repositories;

/// <summary>
/// Write repository for tour aggregates. Handles Add, Update, and entity loading for commands.
/// </summary>
public class TourRepository : ITourRepository
{
    private readonly CatalogDbContext _db;

    public TourRepository(CatalogDbContext db)
    {
        _db = db;
    }

    public Task<Tour?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Tours
            .Include(t => t.Offers)
            .Include(t => t.Days)
            .Include(t => t.Inclusions)
            .Include(t => t.Images)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task AddAsync(Tour tour, CancellationToken ct = default)
        => _db.Tours.AddAsync(tour, ct).AsTask();

    public void Update(Tour tour)
        => _db.Tours.Update(tour);

    public Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default)
        => _db.Tours.AnyAsync(t => t.Title == title, ct);
}
