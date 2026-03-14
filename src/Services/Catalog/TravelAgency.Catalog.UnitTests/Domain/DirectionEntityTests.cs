using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.UnitTests.Domain;

public class DirectionEntityTests
{
    [Fact]
    public void Create_WithValidData_ReturnsDirection()
    {
        var direction = Direction.Create("Greek Islands", "Greece", "Beautiful island destinations");

        direction.Id.Should().NotBeEmpty();
        direction.Name.Should().Be("Greek Islands");
        direction.Country.Should().Be("Greece");
        direction.Description.Should().Be("Beautiful island destinations");
        direction.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsCatalogDomainException()
    {
        var act = () => Direction.Create("", "Greece", null);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Create_WithEmptyCountry_ThrowsCatalogDomainException()
    {
        var act = () => Direction.Create("Greek Islands", "", null);

        act.Should().Throw<CatalogDomainException>();
    }

    [Fact]
    public void Update_ChangesNameAndCountry()
    {
        var direction = Direction.Create("Greek Islands", "Greece", "Old description");

        direction.Update("Italian Riviera", "Italy", "New description");

        direction.Name.Should().Be("Italian Riviera");
        direction.Country.Should().Be("Italy");
        direction.Description.Should().Be("New description");
        direction.UpdatedAt.Should().NotBeNull();
    }
}
