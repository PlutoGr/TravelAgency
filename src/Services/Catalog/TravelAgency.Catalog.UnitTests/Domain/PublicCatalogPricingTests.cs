using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.UnitTests.Domain;

public class PublicCatalogPricingTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PriceFrom_UsesCheapestFutureDeparture_AndIgnoresEarlierCheapOffer()
    {
        var tourId = Guid.NewGuid();
        var offers = new[]
        {
            Offer(tourId, Now.AddDays(-10), 10m),
            Offer(tourId, Now.AddDays(4), 80m),
            Offer(tourId, Now.AddDays(20), 50m)
        };

        var today = DateOnly.FromDateTime(Now);
        PublicCatalogPricing.PriceFrom(offers, today).Should().Be(50m);
        PublicCatalogPricing.CurrencyOfPriceFrom(offers, today).Should().Be("EUR");
        PublicCatalogPricing.NearestDeparture(offers, today).Should().Be(DateOnly.FromDateTime(Now.AddDays(4)));
    }

    [Fact]
    public void PriceFrom_IncludesDepartureOnToday_AndIgnoresThePreviousDay()
    {
        var tourId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(Now);
        var offers = new[]
        {
            Offer(tourId, Now, 40m),
            Offer(tourId, Now.AddDays(-1), 15m)
        };

        PublicCatalogPricing.PriceFrom(offers, today).Should().Be(40m);
        PublicCatalogPricing.NearestDeparture(offers, today).Should().Be(today);
        PublicCatalogPricing.FutureOffers(offers, today).Should().ContainSingle();
    }

    [Fact]
    public void PriceFrom_WhenDepartureIsBeforeToday_ReturnsNull()
    {
        var tourId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(Now);
        var offers = new[]
        {
            Offer(tourId, Now.AddDays(-1), 15m)
        };

        PublicCatalogPricing.PriceFrom(offers, today).Should().BeNull();
        PublicCatalogPricing.NearestDeparture(offers, today).Should().BeNull();
        PublicCatalogPricing.FutureOffers(offers, today).Should().BeEmpty();
    }

    private static TourOffer Offer(Guid tourId, DateTime validFrom, decimal price) =>
        TourOffer.Create(tourId, validFrom, validFrom.AddDays(2), price, "EUR", 2);
}
