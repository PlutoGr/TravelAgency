using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.UnitTests.Domain;

public class TourEntityTests
{
    [Fact]
    public void Create_WithValidData_ReturnsTour()
    {
        var tour = Tour.Create("Santorini Explorer", "Beautiful Greek island tour", TourType.Beach, "Greece", 7, null, null);

        tour.Id.Should().NotBeEmpty();
        tour.Title.Should().Be("Santorini Explorer");
        tour.Country.Should().Be("Greece");
        tour.DurationDays.Should().Be(7);
        tour.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsCatalogDomainException()
    {
        var act = () => Tour.Create("", "Description", TourType.Beach, "Greece", 7, null, null);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithEmptyCountry_ThrowsCatalogDomainException()
    {
        var act = () => Tour.Create("Santorini Explorer", "Description", TourType.Beach, "", 7, null, null);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithZeroDurationDays_ThrowsCatalogDomainException()
    {
        var act = () => Tour.Create("Santorini Explorer", "Description", TourType.Beach, "Greece", 0, null, null);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var tour = Tour.Create("Old Title", "Old Description", TourType.Beach, "Greece", 7, null, null);

        tour.Update("New Title", "New Description", TourType.Mountain, "Italy", 10, null, null);

        tour.Title.Should().Be("New Title");
        tour.Country.Should().Be("Italy");
        tour.DurationDays.Should().Be(10);
        tour.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetPrices_ReplacesPrices()
    {
        var tour = Tour.Create("Test Tour", "Description", TourType.Beach, "Greece", 7, null, null);
        var validFrom = DateTime.UtcNow.AddDays(1);

        var initialPrices = new[]
        {
            TourPrice.Create(tour.Id, validFrom, validFrom.AddDays(7), 500m, "USD", 10),
            TourPrice.Create(tour.Id, validFrom.AddDays(10), validFrom.AddDays(17), 600m, "USD", 5)
        };
        tour.SetPrices(initialPrices);

        var newPrice = TourPrice.Create(tour.Id, validFrom.AddDays(20), validFrom.AddDays(27), 700m, "USD", 8);
        tour.SetPrices([newPrice]);

        tour.Prices.Should().HaveCount(1);
        tour.Prices.First().PricePerPerson.Should().Be(700m);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var tour = Tour.Create("Test Tour", "Description", TourType.Beach, "Greece", 7, null, null);

        tour.Deactivate();

        tour.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var tour = Tour.Create("Test Tour", "Description", TourType.Beach, "Greece", 7, null, null);
        tour.Deactivate();

        tour.Activate();

        tour.IsActive.Should().BeTrue();
    }
}
