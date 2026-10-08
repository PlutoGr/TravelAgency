using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.Mappings;

public static class PublicMediaUrls
{
    public const string CardSize = "w800";

    public static string File(Guid mediaFileId, string size = CardSize) =>
        $"/api/v1/media/files/{mediaFileId:D}/{size}";
}

public static class PublicCatalogMapper
{
    public static TourSummaryDto ToSummary(Tour tour, DateTime utcNow)
    {
        var previews = ToPreviews(tour.Images, PublicCatalogLimits.MaxListPreviews);
        var cover = previews.FirstOrDefault(preview => preview.IsCover) ?? previews.FirstOrDefault();
        var priceFrom = PublicCatalogPricing.PriceFrom(tour.Offers, utcNow);

        return new TourSummaryDto(
            tour.Id,
            tour.Title,
            tour.Country,
            tour.TourType,
            tour.DurationDays,
            cover?.Url ?? tour.ImageUrl,
            priceFrom,
            PublicCatalogPricing.CurrencyOfPriceFrom(tour.Offers, utcNow),
            tour.Status == TourStatus.Published,
            priceFrom,
            PublicCatalogPricing.NearestDeparture(tour.Offers, utcNow),
            tour.ShortDescription,
            tour.DepartureCity,
            previews);
    }

    public static PublicTourDto ToPublicTour(Tour tour, DateTime utcNow)
    {
        var images = ToPreviews(tour.Images, limit: null);
        var cover = images.FirstOrDefault(image => image.IsCover) ?? images.FirstOrDefault();

        return new PublicTourDto(
            tour.Id,
            tour.Title,
            tour.ShortDescription,
            tour.Description,
            tour.DepartureCity,
            tour.Country,
            tour.TourType,
            tour.DurationDays,
            cover?.Url ?? tour.ImageUrl,
            tour.DirectionId,
            tour.Status == TourStatus.Published,
            tour.CreatedAt,
            tour.UpdatedAt,
            tour.PublishedAt,
            tour.MealPlan?.ToString(),
            tour.AccommodationText,
            PublicCatalogPricing.PriceFrom(tour.Offers, utcNow),
            PublicCatalogPricing.CurrencyOfPriceFrom(tour.Offers, utcNow),
            PublicCatalogPricing.NearestDeparture(tour.Offers, utcNow),
            PublicCatalogPricing.FutureOffers(tour.Offers, utcNow)
                .Select(TourMapper.ToPriceDto)
                .ToList(),
            tour.Days
                .OrderBy(day => day.DayNumber)
                .Select(day => new PublicTourDayDto(day.DayNumber, day.Title, day.Description))
                .ToList(),
            tour.Inclusions
                .OrderBy(item => item.SortOrder)
                .Select(item => new PublicTourInclusionDto(item.Text, item.Kind.ToString(), item.SortOrder))
                .ToList(),
            images,
            []);
    }

    /// <summary>
    /// Снятый тур отдаёт только id, название, обложку и available=false.
    /// Цена и описание в эту карточку не попадают.
    /// </summary>
    public static PublicTourCardDto ToCard(Tour tour, DateTime utcNow)
    {
        var coverImage = tour.Images.FirstOrDefault(image => image.IsCover)
            ?? tour.Images.OrderBy(image => image.SortOrder).ThenBy(image => image.Id).FirstOrDefault();
        var cover = coverImage is null ? null : ToPreview(coverImage);

        if (tour.Status != TourStatus.Published)
        {
            return new PublicTourCardDto(
                tour.Id,
                tour.Title,
                cover,
                false,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        return new PublicTourCardDto(
            tour.Id,
            tour.Title,
            cover,
            true,
            PublicCatalogPricing.PriceFrom(tour.Offers, utcNow),
            PublicCatalogPricing.CurrencyOfPriceFrom(tour.Offers, utcNow),
            PublicCatalogPricing.NearestDeparture(tour.Offers, utcNow),
            tour.ShortDescription,
            tour.Country,
            tour.DurationDays);
    }

    public static IReadOnlyList<PublicTourPreviewDto> ToPreviews(IEnumerable<TourImage> images, int? limit)
    {
        IEnumerable<TourImage> ordered = images
            .OrderByDescending(image => image.IsCover)
            .ThenBy(image => image.SortOrder)
            .ThenBy(image => image.Id);

        if (limit is int take)
            ordered = ordered.Take(take);

        return ordered.Select(ToPreview).ToList();
    }

    public static PublicTourPreviewDto ToPreview(TourImage image) =>
        new(
            image.MediaFileId,
            PublicMediaUrls.File(image.MediaFileId),
            image.Alt,
            image.IsCover,
            image.SortOrder);
}
