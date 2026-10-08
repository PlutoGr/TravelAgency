using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.Infrastructure.Queries;

public sealed class PublicTourCardQuery(CatalogDbContext db) : IPublicTourCardQuery
{
    public async Task<IReadOnlyList<Tour>> GetPublishedOrUnpublishedAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await db.Tours
            .AsNoTracking()
            .AsSplitQuery()
            .Include(tour => tour.Offers)
            .Include(tour => tour.Images)
            .Where(tour => ids.Contains(tour.Id)
                && (tour.Status == TourStatus.Published || tour.Status == TourStatus.Unpublished))
            .ToListAsync(cancellationToken);
    }
}
