using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.Infrastructure.Repositories;

public class DirectionRepository : IDirectionRepository
{
    private readonly CatalogDbContext _db;

    public DirectionRepository(CatalogDbContext db)
    {
        _db = db;
    }

    public Task<List<Direction>> GetAllActiveAsync(CancellationToken ct = default)
        => _db.Directions.Where(d => d.IsActive).ToListAsync(ct);

    public Task<Direction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Directions.FirstOrDefaultAsync(d => d.Id == id, ct);
}
