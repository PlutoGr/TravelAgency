using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourCards;
using TravelAgency.Catalog.UnitTests.Domain;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class GetTourCardsQueryHandlerTests
{
    [Fact]
    public async Task Handle_PreservesRequestOrder_SkipsDraftsAndUnknownIds_AndMarksUnpublishedUnavailable()
    {
        var now = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        var published = PublishableTourFactory.Create("Видимый");
        published.Publish(now, 1280);
        var hidden = PublishableTourFactory.Create("Снятый");
        hidden.Publish(now, 1280);
        hidden.Unpublish();
        var draft = Tour.Create("Черновик", "Описание", TourType.City, "Италия", 3, null);

        var query = new Mock<IPublicTourCardQuery>();
        query.Setup(cards => cards.GetPublishedOrUnpublishedAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([draft, published, hidden]);

        var missing = Guid.NewGuid();
        var handler = new GetTourCardsQueryHandler(query.Object);
        var result = await handler.Handle(
            new GetTourCardsQuery([hidden.Id.ToString(), published.Id.ToString(), draft.Id.ToString(), missing.ToString()]),
            CancellationToken.None);

        result.Select(card => card.Id).Should().Equal(hidden.Id, published.Id);
        result[0].Available.Should().BeFalse();
        result[0].PriceFrom.Should().BeNull();
        result[1].Available.Should().BeTrue();
        result[1].PriceFrom.Should().Be(1000m);
    }
}
