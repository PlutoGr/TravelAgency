using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Manage;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Mappings;

public static class TourManageMapper
{
    public static TourManageDto ToManageDto(Tour tour) =>
        new(
            tour.Id,
            tour.Title,
            tour.ShortDescription,
            tour.Description,
            tour.DepartureCity,
            tour.Country,
            tour.TourType.ToString(),
            tour.DurationDays,
            tour.DirectionId,
            tour.MealPlan?.ToString(),
            tour.AccommodationText,
            tour.Status.ToString(),
            tour.Source.ToString(),
            tour.OwnerId,
            tour.Version,
            TourEtag.Format(tour.Version),
            tour.Days
                .OrderBy(d => d.DayNumber)
                .Select(d => new TourDayItemDto(d.Id, d.DayNumber, d.Title, d.Description))
                .ToList(),
            tour.Inclusions
                .OrderBy(i => i.SortOrder)
                .Select(i => new TourInclusionItemDto(i.Id, i.Text, i.Kind.ToString(), i.SortOrder))
                .ToList(),
            tour.Offers
                .OrderBy(o => o.ValidFrom)
                .Select(o => new TourOfferItemDto(
                    o.Id, o.ValidFrom, o.ValidTo, o.PricePerPerson, o.Currency, o.AvailableSeats))
                .ToList(),
            tour.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new TourImageItemDto(i.Id, i.MediaFileId, i.SortOrder, i.IsCover, i.Alt, i.WidthPx))
                .ToList());
}
