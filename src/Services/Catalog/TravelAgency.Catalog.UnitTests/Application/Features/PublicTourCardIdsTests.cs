using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourCards;
using TravelAgency.Catalog.Domain;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class PublicTourCardIdsTests
{
    [Fact]
    public void TryParse_SplitsCommas_AndDropsDuplicates_KeepingOrder()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var parsed = PublicTourCardIds.TryParse(
            [$"{first}, {second}", first.ToString()],
            out var ids,
            out var error);

        parsed.Should().BeTrue();
        error.Should().BeNull();
        ids.Should().Equal(first, second);
    }

    [Fact]
    public void TryParse_RejectsMoreThanFiftyIds_AndInvalidTokens()
    {
        var tooMany = Enumerable.Range(0, PublicCatalogLimits.MaxCardIds + 1)
            .Select(_ => Guid.NewGuid().ToString())
            .ToArray();

        PublicTourCardIds.TryParse(tooMany, out _, out var limitError).Should().BeFalse();
        limitError.Should().NotBeNullOrWhiteSpace();

        PublicTourCardIds.TryParse(["not-a-guid"], out _, out var formatError).Should().BeFalse();
        formatError.Should().NotBeNullOrWhiteSpace();
    }
}
