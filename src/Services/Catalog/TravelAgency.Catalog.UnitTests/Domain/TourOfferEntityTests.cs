using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.UnitTests.Domain;

public class TourOfferEntityTests
{
    private static readonly DateTime ValidFrom = DateTime.UtcNow.AddDays(1);
    private static readonly DateTime ValidTo = DateTime.UtcNow.AddDays(8);

    [Fact]
    public void Create_WithValidData_ReturnsTourOffer()
    {
        var tourId = Guid.NewGuid();

        var offer = TourOffer.Create(tourId, ValidFrom, ValidTo, 999.99m, "EUR", 20);

        offer.Id.Should().NotBeEmpty();
        offer.TourId.Should().Be(tourId);
        offer.ValidFrom.Should().Be(ValidFrom);
        offer.ValidTo.Should().Be(ValidTo);
        offer.PricePerPerson.Should().Be(999.99m);
        offer.Currency.Should().Be("EUR");
        offer.AvailableSeats.Should().Be(20);
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsCatalogDomainException()
    {
        var act = () => TourOffer.Create(Guid.NewGuid(), ValidFrom, ValidTo, -100m, "USD", 10);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithZeroPrice_ThrowsCatalogDomainException()
    {
        var act = () => TourOffer.Create(Guid.NewGuid(), ValidFrom, ValidTo, 0m, "USD", 10);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithNegativeSeats_ThrowsCatalogDomainException()
    {
        var act = () => TourOffer.Create(Guid.NewGuid(), ValidFrom, ValidTo, 500m, "USD", -1);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithValidFromAfterValidTo_ThrowsCatalogDomainException()
    {
        var from = DateTime.UtcNow.AddDays(10);
        var to = DateTime.UtcNow.AddDays(5);

        var act = () => TourOffer.Create(Guid.NewGuid(), from, to, 500m, "USD", 10);

        act.Should().Throw<CatalogDomainException>();
    }
}
