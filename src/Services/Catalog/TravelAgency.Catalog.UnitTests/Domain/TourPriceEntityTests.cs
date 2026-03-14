using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.UnitTests.Domain;

public class TourPriceEntityTests
{
    private static readonly DateTime ValidFrom = DateTime.UtcNow.AddDays(1);
    private static readonly DateTime ValidTo = DateTime.UtcNow.AddDays(8);

    [Fact]
    public void Create_WithValidData_ReturnsTourPrice()
    {
        var tourId = Guid.NewGuid();

        var price = TourPrice.Create(tourId, ValidFrom, ValidTo, 999.99m, "EUR", 20);

        price.Id.Should().NotBeEmpty();
        price.TourId.Should().Be(tourId);
        price.ValidFrom.Should().Be(ValidFrom);
        price.ValidTo.Should().Be(ValidTo);
        price.PricePerPerson.Should().Be(999.99m);
        price.Currency.Should().Be("EUR");
        price.AvailableSeats.Should().Be(20);
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsCatalogDomainException()
    {
        var act = () => TourPrice.Create(Guid.NewGuid(), ValidFrom, ValidTo, -100m, "USD", 10);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithZeroPrice_ThrowsCatalogDomainException()
    {
        var act = () => TourPrice.Create(Guid.NewGuid(), ValidFrom, ValidTo, 0m, "USD", 10);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithNegativeSeats_ThrowsCatalogDomainException()
    {
        var act = () => TourPrice.Create(Guid.NewGuid(), ValidFrom, ValidTo, 500m, "USD", -1);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithValidFromAfterValidTo_ThrowsCatalogDomainException()
    {
        var from = DateTime.UtcNow.AddDays(10);
        var to = DateTime.UtcNow.AddDays(5);

        var act = () => TourPrice.Create(Guid.NewGuid(), from, to, 500m, "USD", 10);

        act.Should().Throw<CatalogDomainException>();
    }
}
