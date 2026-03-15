using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class GetToursQueryHandlerTests
{
    private readonly Mock<ITourRepository> _tourRepositoryMock = new();
    private readonly GetToursQueryHandler _handler;

    public GetToursQueryHandlerTests()
    {
        _handler = new GetToursQueryHandler(_tourRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var filter = new ToursFilterDto(Page: 1, PageSize: 10);
        var items = new List<TourSummaryDto>
        {
            new(Guid.NewGuid(), "Tour 1", "Greece", TourType.Beach, 7, null, 1000m, "USD", true)
        };
        var expected = new PagedResult<TourSummaryDto>(items, 1, 1, 10);

        _tourRepositoryMock
            .Setup(r => r.GetPagedAsync(It.IsAny<ToursFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _handler.Handle(new GetToursQuery(filter), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyItems()
    {
        var filter = new ToursFilterDto(Page: 1, PageSize: 20);
        var expected = new PagedResult<TourSummaryDto>([], 0, 1, 20);

        _tourRepositoryMock
            .Setup(r => r.GetPagedAsync(It.IsAny<ToursFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _handler.Handle(new GetToursQuery(filter), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PassesFilterToRepository()
    {
        var filter = new ToursFilterDto(Country: "Greece", DirectionId: Guid.NewGuid(), Page: 2, PageSize: 5);
        var expected = new PagedResult<TourSummaryDto>([], 0, 2, 5);

        _tourRepositoryMock
            .Setup(r => r.GetPagedAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        await _handler.Handle(new GetToursQuery(filter), CancellationToken.None);

        _tourRepositoryMock.Verify(
            r => r.GetPagedAsync(filter, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
