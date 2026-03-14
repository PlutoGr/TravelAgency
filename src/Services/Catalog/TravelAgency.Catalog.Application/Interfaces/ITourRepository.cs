using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Interfaces;

public interface ITourRepository
{
    Task<Tour?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<TourSummaryDto>> GetPagedAsync(ToursFilterDto filter, CancellationToken ct = default);
    Task AddAsync(Tour tour, CancellationToken ct = default);
    void Update(Tour tour);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
}
