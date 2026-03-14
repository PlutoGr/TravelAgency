using TravelAgency.Catalog.Application.Features.Directions.Queries.GetDirections;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class GetDirectionsQueryHandlerTests
{
    private readonly Mock<IDirectionRepository> _directionRepositoryMock = new();
    private readonly GetDirectionsQueryHandler _handler;

    public GetDirectionsQueryHandlerTests()
    {
        _handler = new GetDirectionsQueryHandler(_directionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllActiveDirections()
    {
        var directions = new List<Direction>
        {
            Direction.Create("Greek Islands", "Greece", "Beautiful islands"),
            Direction.Create("Amalfi Coast", "Italy", "Stunning coastal views")
        };

        _directionRepositoryMock
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directions);

        var result = await _handler.Handle(new GetDirectionsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Greek Islands");
        result[0].Country.Should().Be("Greece");
        result[1].Name.Should().Be("Amalfi Coast");
        result[1].Country.Should().Be("Italy");
    }
}
