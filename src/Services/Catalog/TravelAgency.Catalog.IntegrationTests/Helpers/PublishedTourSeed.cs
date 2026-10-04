using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.IntegrationTests.Helpers;

internal static class PublishedTourSeed
{
    public static Tour Create(
        string title,
        string description,
        int durationDays,
        decimal currentPrice,
        string currency,
        int seats)
    {
        var tour = Tour.Create(title, description, TourType.Beach, "Spain", durationDays, null);
        tour.SetShortDescription("Short description");
        tour.ReplaceDays(Enumerable.Range(1, durationDays)
            .Select(day => TourDay.Create(tour.Id, day, $"Day {day}", "Program")));
        tour.ReplaceInclusions([TourInclusion.Create(tour.Id, "Flight", TourInclusionKind.Included)]);
        tour.SetConditions(MealPlan.BB, "Hotel");

        var now = DateTime.UtcNow;
        var offers = new List<TourOffer>
        {
            TourOffer.Create(tour.Id, now.AddDays(-1), now.AddDays(20), currentPrice, currency, seats),
            TourOffer.Create(tour.Id, now.AddDays(30), now.AddDays(40), currentPrice + 100m, currency, seats)
        };
        tour.ReplaceOffers(offers);
        tour.ReplaceImages(
        [
            TourImage.Create(tour.Id, Guid.NewGuid(), 0, true, "Cover", TourContentLimits.CoverMinWidthPx),
            TourImage.Create(tour.Id, Guid.NewGuid(), 1, false, "Two"),
            TourImage.Create(tour.Id, Guid.NewGuid(), 2, false, "Three")
        ]);
        tour.Publish(now, TourContentLimits.CoverMinWidthPx);
        return tour;
    }
}
