using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Domain.Interfaces;

public interface ITourRepository
{
    Task<Tour?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Tour tour, CancellationToken ct = default);
    void Update(Tour tour);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
}
