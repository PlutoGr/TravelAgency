using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.UnitTests.Domain;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourById;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Interfaces;

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
    public async Task Handle_WhenPublished_ReturnsPageWithEmptyComponents()
    {
        var now = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        var tour = PublishableTourFactory.Create("Test Tour");
        tour.Publish(now, 1280);

        _tourRepositoryMock
            .Setup(repository => repository.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tour);

        var result = await _handler.Handle(new GetTourByIdQuery(tour.Id), CancellationToken.None);

        result.Id.Should().Be(tour.Id);
        result.Title.Should().Be("Test Tour");
        result.Country.Should().Be("Greece");
        result.Components.Should().BeEmpty();
        result.Days.Should().NotBeEmpty();
        result.PriceFrom.Should().Be(1000m);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenNotPublished_ThrowsNotFound(bool unpublish)
    {
        var now = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        var tour = PublishableTourFactory.Create();
        if (unpublish)
        {
            tour.Publish(now, 1280);
            tour.Unpublish();
        }

        _tourRepositoryMock
            .Setup(repository => repository.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tour);

        var act = async () => await _handler.Handle(new GetTourByIdQuery(tour.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNonExistingTour_ThrowsNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();

        _tourRepositoryMock
            .Setup(repository => repository.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tour?)null);

        var act = async () => await _handler.Handle(new GetTourByIdQuery(nonExistentId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
