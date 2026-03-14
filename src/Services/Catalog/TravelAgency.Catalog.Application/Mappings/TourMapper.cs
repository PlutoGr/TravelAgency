using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Mappings;

public static class TourMapper
{
    public static TourDto ToDto(Tour tour) =>
        new(
            tour.Id,
            tour.Title,
            tour.Description,
            tour.Country,
            tour.TourType,
            tour.DurationDays,
            tour.ImageUrl,
            tour.DirectionId,
            tour.IsActive,
            tour.CreatedAt,
            tour.UpdatedAt,
            tour.Prices.Select(ToPriceDto).ToList().AsReadOnly());

    public static TourSummaryDto ToSummaryDto(Tour tour)
    {
        var cheapest = tour.Prices
            .OrderBy(p => p.PricePerPerson)
            .FirstOrDefault();

        return new TourSummaryDto(
            tour.Id,
            tour.Title,
            tour.Country,
            tour.TourType,
            tour.DurationDays,
            tour.ImageUrl,
            cheapest?.PricePerPerson,
            cheapest?.Currency,
            tour.IsActive);
    }

    public static TourPriceDto ToPriceDto(TourPrice price) =>
        new(
            price.Id,
            price.ValidFrom,
            price.ValidTo,
            price.PricePerPerson,
            price.Currency,
            price.AvailableSeats);
}
