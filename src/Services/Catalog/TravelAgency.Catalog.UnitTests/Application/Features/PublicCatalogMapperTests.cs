using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.UnitTests.Domain;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class PublicCatalogMapperTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ToPublicTour_ShowsPublishedContent_PriceFromFutureOnly_AndEmptyComponents()
    {
        var tour = PublishableTourFactory.Create(durationDays: 2);
        var past = Now.AddDays(-8);
        tour.ReplaceOffers(
        [
            TourOffer.Create(tour.Id, past, past.AddDays(2), 10m, "EUR", 1),
            TourOffer.Create(tour.Id, Now.AddDays(3), Now.AddDays(5), 90m, "EUR", 2),
            TourOffer.Create(tour.Id, Now.AddDays(12), Now.AddDays(14), 40m, "EUR", 2)
        ]);
        tour.Publish(Now, 1280);

        var page = PublicCatalogMapper.ToPublicTour(tour, Now);

        page.Components.Should().BeEmpty();
        page.Days.Should().HaveCount(2);
        page.Inclusions.Should().Contain(item => item.Kind == nameof(TourInclusionKind.Included));
        page.MealPlan.Should().Be(nameof(MealPlan.BB));
        page.AccommodationText.Should().Be("Hotel 4*");
        page.DepartureCity.Should().Be("Moscow");
        page.PriceFrom.Should().Be(40m);
        page.NearestDate.Should().Be(Now.AddDays(3));
        page.Prices.Should().OnlyContain(price => price.PricePerPerson >= 40m);
        page.Prices.Select(price => price.PricePerPerson).Should().NotContain(10m);
        page.Images.Should().NotBeEmpty();
        page.CoverMediaFileId.Should().Be(page.Images.First(image => image.IsCover).MediaFileId);
    }

    [Fact]
    public void ToSummary_LimitsPreviewsToFive_AndKeepsCover()
    {
        var tour = PublishableTourFactory.Create(durationDays: 1);
        var images = new List<TourImage>
        {
            TourImage.Create(tour.Id, Guid.NewGuid(), 0, true, "Cover", 1600)
        };
        for (var index = 1; index < 6; index++)
            images.Add(TourImage.Create(tour.Id, Guid.NewGuid(), index, false, $"Photo {index}"));
        tour.ReplaceImages(images, 1600);
        tour.Publish(Now, 1600);

        var summary = PublicCatalogMapper.ToSummary(tour, Now);

        summary.Previews.Should().NotBeNull();
        summary.Previews.Should().HaveCount(PublicCatalogLimits.MaxListPreviews);
        summary.Previews![0].IsCover.Should().BeTrue();
        summary.PriceFrom.Should().Be(summary.MinPrice);
    }

    [Fact]
    public void ToCard_Unpublished_KeepsTitleAndCover_AndHidesPrice()
    {
        var tour = PublishableTourFactory.Create();
        tour.Publish(Now, 1280);
        tour.Unpublish();

        var card = PublicCatalogMapper.ToCard(tour, Now);

        card.Available.Should().BeFalse();
        card.Title.Should().Be(tour.Title);
        card.Cover.Should().NotBeNull();
        card.PriceFrom.Should().BeNull();
        card.Country.Should().BeNull();
        card.NearestDate.Should().BeNull();
        card.ShortDescription.Should().BeNull();
    }
}
