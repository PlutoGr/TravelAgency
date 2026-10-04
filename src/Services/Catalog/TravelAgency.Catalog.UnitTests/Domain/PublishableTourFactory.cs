using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Domain;

internal static class PublishableTourFactory
{
    public static Tour Create(string title = "Complete tour", int durationDays = 2, int? coverWidthPx = 1280)
    {
        var tour = Tour.Create(title, "Full description of the tour", TourType.Beach, "Greece", durationDays, null);
        tour.SetShortDescription("Short description");
        tour.SetDepartureCity("Moscow");
        tour.ReplaceDays(Enumerable.Range(1, durationDays)
            .Select(day => TourDay.Create(tour.Id, day, $"Day {day}", $"Program for day {day}")));
        tour.ReplaceInclusions(
        [
            TourInclusion.Create(tour.Id, "Flight", TourInclusionKind.Included, 0),
            TourInclusion.Create(tour.Id, "Visa", TourInclusionKind.NotIncluded, 1)
        ]);
        tour.SetConditions(MealPlan.BB, "Hotel 4*");
        var from = DateTime.UtcNow.AddDays(10);
        tour.ReplaceOffers(
        [
            TourOffer.Create(tour.Id, from, from.AddDays(durationDays), 1000m, "EUR", 8)
        ]);
        tour.ReplaceImages(
        [
            TourImage.Create(tour.Id, Guid.NewGuid(), 0, true, "Cover", coverWidthPx),
            TourImage.Create(tour.Id, Guid.NewGuid(), 1, false, "Second"),
            TourImage.Create(tour.Id, Guid.NewGuid(), 2, false, "Third")
        ]);
        return tour;
    }
}
