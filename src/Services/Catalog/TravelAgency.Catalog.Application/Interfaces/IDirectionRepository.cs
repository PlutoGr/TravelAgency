using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Interfaces;

public interface IDirectionRepository
{
    Task<List<Direction>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Direction?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
