using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.Infrastructure.Queries;

/// <summary>
/// Публичный список. В выдаче только Published: параметр IsActive не открывает черновики и снятые.
/// Цена «от», фильтр цены и сортировка по цене смотрят на будущие выезды (ValidFrom позже utcNow).
/// </summary>
public class TourListQuery(CatalogDbContext db) : ITourListQuery
{
    public async Task<PagedResult<TourSummaryDto>> GetPagedAsync(ToursFilterDto filter, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var filtered = ApplyFilters(db.Tours.AsNoTracking(), filter, now);
        var totalCount = await filtered.CountAsync(ct);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var pageIds = await Sort(filtered, filter, now)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tour => tour.Id)
            .ToListAsync(ct);

        if (pageIds.Count == 0)
            return new PagedResult<TourSummaryDto>([], totalCount, page, pageSize);

        var tours = await db.Tours
            .AsNoTracking()
            .AsSplitQuery()
            .Include(tour => tour.Offers)
            .Include(tour => tour.Images)
            .Where(tour => pageIds.Contains(tour.Id))
            .ToListAsync(ct);

        var byId = tours.ToDictionary(tour => tour.Id);
        var items = new List<TourSummaryDto>(pageIds.Count);
        foreach (var id in pageIds)
        {
            if (byId.TryGetValue(id, out var tour))
                items.Add(PublicCatalogMapper.ToSummary(tour, now));
        }

        return new PagedResult<TourSummaryDto>(items, totalCount, page, pageSize);
    }

    private static IQueryable<Tour> ApplyFilters(IQueryable<Tour> query, ToursFilterDto filter, DateTime now)
    {
        query = query.Where(tour => tour.Status == TourStatus.Published);

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            var country = filter.Country.Trim().ToLower();
            query = query.Where(tour => tour.Country.ToLower().Contains(country));
        }

        if (filter.DirectionId is Guid directionId)
            query = query.Where(tour => tour.DirectionId == directionId);

        if (filter.TourType is TourType tourType)
            query = query.Where(tour => tour.TourType == tourType);

        if (filter.MinPrice is decimal minPrice)
        {
            query = query.Where(tour =>
                tour.Offers.Where(offer => offer.ValidFrom > now)
                    .Select(offer => (decimal?)offer.PricePerPerson)
                    .Min() >= minPrice);
        }

        if (filter.MaxPrice is decimal maxPrice)
        {
            query = query.Where(tour =>
                tour.Offers.Where(offer => offer.ValidFrom > now)
                    .Select(offer => (decimal?)offer.PricePerPerson)
                    .Min() <= maxPrice);
        }

        if (filter.DateFrom is DateTime dateFrom && filter.DateTo is DateTime dateTo)
        {
            query = query.Where(tour => tour.Offers.Any(offer =>
                offer.ValidFrom > now && offer.ValidFrom >= dateFrom && offer.ValidFrom <= dateTo));
        }
        else if (filter.DateFrom is DateTime fromOnly)
        {
            query = query.Where(tour => tour.Offers.Any(offer =>
                offer.ValidFrom > now && offer.ValidFrom >= fromOnly));
        }
        else if (filter.DateTo is DateTime toOnly)
        {
            query = query.Where(tour => tour.Offers.Any(offer =>
                offer.ValidFrom > now && offer.ValidFrom <= toOnly));
        }

        return query;
    }

    private static IOrderedQueryable<Tour> Sort(IQueryable<Tour> query, ToursFilterDto filter, DateTime now)
    {
        return filter.SortBy switch
        {
            TourSortBy.Title => filter.SortDirection == SortDirection.Asc
                ? query.OrderBy(tour => tour.Title).ThenBy(tour => tour.Id)
                : query.OrderByDescending(tour => tour.Title).ThenBy(tour => tour.Id),
            TourSortBy.DurationDays => filter.SortDirection == SortDirection.Asc
                ? query.OrderBy(tour => tour.DurationDays).ThenBy(tour => tour.Id)
                : query.OrderByDescending(tour => tour.DurationDays).ThenBy(tour => tour.Id),
            TourSortBy.Price => filter.SortDirection == SortDirection.Asc
                ? query.OrderBy(tour => tour.Offers.Where(offer => offer.ValidFrom > now)
                        .Select(offer => (decimal?)offer.PricePerPerson)
                        .Min() ?? decimal.MaxValue)
                    .ThenBy(tour => tour.Id)
                : query.OrderByDescending(tour => tour.Offers.Where(offer => offer.ValidFrom > now)
                        .Select(offer => (decimal?)offer.PricePerPerson)
                        .Min() ?? decimal.MinValue)
                    .ThenBy(tour => tour.Id),
            _ => filter.SortDirection == SortDirection.Asc
                ? query.OrderBy(tour => tour.CreatedAt).ThenBy(tour => tour.Id)
                : query.OrderByDescending(tour => tour.CreatedAt).ThenBy(tour => tour.Id)
        };
    }
}
