using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Mappings;

public static class TourMapper
{
    public static TourSummaryDto ToSummaryDto(Tour tour) =>
        PublicCatalogMapper.ToSummary(tour, DateTime.UtcNow);

    public static TourPriceDto ToPriceDto(TourOffer price) =>
        new(
            price.Id,
            price.ValidFrom,
            price.ValidTo,
            price.PricePerPerson,
            price.Currency,
            price.AvailableSeats);
}
