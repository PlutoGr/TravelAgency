using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

namespace TravelAgency.Catalog.UnitTests.Infrastructure;

public class TourXminConfigurationTests
{
    [Fact]
    public void Tour_UsesPostgresXminAsConcurrencyToken()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=catalog_model;Username=user")
            .Options;

        using var db = new CatalogDbContext(options);
        var tour = db.Model.FindEntityType(typeof(Tour));
        var xmin = tour!.FindProperty("xmin");

        xmin.Should().NotBeNull();
        xmin!.IsConcurrencyToken.Should().BeTrue();
        xmin.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
    }

    [Fact]
    public void TourImage_HasFilteredUniqueIndexForSingleCover()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=catalog_model;Username=user")
            .Options;

        using var db = new CatalogDbContext(options);
        var image = db.Model.FindEntityType(typeof(TourImage));
        var index = image!.GetIndexes()
            .Single(i => i.GetDatabaseName() == TourImageConfiguration.OneCoverIndexName);

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("\"IsCover\"");
        index.Properties.Select(p => p.Name).Should().Equal("TourId");
    }
}
