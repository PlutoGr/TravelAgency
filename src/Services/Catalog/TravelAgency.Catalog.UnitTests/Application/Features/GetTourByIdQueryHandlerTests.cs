using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourById;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class GetTourByIdQueryHandlerTests
{
    private readonly Mock<ITourRepository> _tourRepositoryMock = new();
    private readonly GetTourByIdQueryHandler _handler;

    public GetTourByIdQueryHandlerTests()
    {
        _handler = new GetTourByIdQueryHandler(_tourRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingTour_ReturnsTourDto()
    {
        var tour = Tour.Create("Test Tour", "Description", TourType.Beach, "Greece", 7, null, null);
        var query = new GetTourByIdQuery(tour.Id);

        _tourRepositoryMock
            .Setup(r => r.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tour);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(tour.Id);
        result.Title.Should().Be("Test Tour");
        result.Country.Should().Be("Greece");
    }

    [Fact]
    public async Task Handle_WithNonExistingTour_ThrowsNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();
        var query = new GetTourByIdQuery(nonExistentId);

        _tourRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tour?)null);

        var act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
