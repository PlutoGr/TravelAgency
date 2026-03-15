using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Interfaces;

/// <summary>
/// Read-model query for paged tour listings. Keeps DTOs and projections in Application layer.
/// </summary>
public interface ITourListQuery
{
    Task<PagedResult<TourSummaryDto>> GetPagedAsync(ToursFilterDto filter, CancellationToken ct = default);
}
