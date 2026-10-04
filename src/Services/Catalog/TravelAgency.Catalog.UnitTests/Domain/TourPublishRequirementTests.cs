using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.UnitTests.Domain;

public class TourPublishRequirementTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Codes_AreTheSingleListIncludingCoverMinWidth()
    {
        TourPublishRequirementCodes.All.Should().Equal(
            TourPublishRequirementCodes.Title,
            TourPublishRequirementCodes.ShortDescription,
            TourPublishRequirementCodes.Description,
            TourPublishRequirementCodes.ProgramDayCount,
            TourPublishRequirementCodes.InclusionsIncluded,
            TourPublishRequirementCodes.MealPlan,
            TourPublishRequirementCodes.Accommodation,
            TourPublishRequirementCodes.OffersFuture,
            TourPublishRequirementCodes.ImagesCover,
            TourPublishRequirementCodes.ImagesMinCount,
            TourPublishRequirementCodes.ImagesCoverMinWidth);
    }

    [Fact]
    public void Publish_WhenCompleteAndCoverWidthIs1280_SucceedsWithNoMissingCodes()
    {
        var tour = PublishableTourFactory.Create(coverWidthPx: null);
        var missing = tour.GetMissingPublishRequirements(Now, TourContentLimits.CoverMinWidthPx);

        missing.Should().BeEmpty();

        tour.Publish(Now, TourContentLimits.CoverMinWidthPx);

        tour.Status.Should().Be(TourStatus.Published);
        tour.PublishedAt.Should().Be(Now);
        tour.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Publish_WhenCoverMetadataWidthIsAtLeast1280_SucceedsWithoutExplicitArgument()
    {
        var tour = PublishableTourFactory.Create(coverWidthPx: TourContentLimits.CoverMinWidthPx);

        tour.GetMissingPublishRequirements(Now).Should().BeEmpty();

        tour.Publish(Now);

        tour.Status.Should().Be(TourStatus.Published);
    }

    [Fact]
    public void Publish_WhenCoverWidthIsBelow1280_ReportsCoverMinWidthAndStaysDraft()
    {
        var tour = PublishableTourFactory.Create(coverWidthPx: TourContentLimits.CoverMinWidthPx - 1);

        var missing = tour.GetMissingPublishRequirements(Now);
        missing.Should().Equal(TourPublishRequirementCodes.ImagesCoverMinWidth);

        var act = () => tour.Publish(Now);

        act.Should().Throw<TourNotPublishableException>()
            .Which.Missing.Should().Equal(TourPublishRequirementCodes.ImagesCoverMinWidth);
        tour.Status.Should().Be(TourStatus.Draft);
        tour.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Publish_WhenExplicitWidthIsBelow1280_RejectsEvenIfStoredMetadataIsWider()
    {
        var tour = PublishableTourFactory.Create(coverWidthPx: 2000);

        var missing = tour.GetMissingPublishRequirements(Now, TourContentLimits.CoverMinWidthPx - 1);

        missing.Should().Equal(TourPublishRequirementCodes.ImagesCoverMinWidth);
        var act = () => tour.Publish(Now, 1000);
        act.Should().Throw<TourNotPublishableException>();
        tour.Status.Should().Be(TourStatus.Draft);
    }

    [Theory]
    [InlineData(TourPublishRequirementCodes.Title)]
    [InlineData(TourPublishRequirementCodes.ShortDescription)]
    [InlineData(TourPublishRequirementCodes.Description)]
    [InlineData(TourPublishRequirementCodes.ProgramDayCount)]
    [InlineData(TourPublishRequirementCodes.InclusionsIncluded)]
    [InlineData(TourPublishRequirementCodes.MealPlan)]
    [InlineData(TourPublishRequirementCodes.Accommodation)]
    [InlineData(TourPublishRequirementCodes.OffersFuture)]
    [InlineData(TourPublishRequirementCodes.ImagesCover)]
    [InlineData(TourPublishRequirementCodes.ImagesMinCount)]
    [InlineData(TourPublishRequirementCodes.ImagesCoverMinWidth)]
    public void GetMissingPublishRequirements_ReportsEachCode(string code)
    {
        var tour = TourMissingOneRequirement(code);

        tour.GetMissingPublishRequirements(Now, CoverWidthFor(code))
            .Should().Equal(code);
    }

    [Fact]
    public void SetShortDescription_WhenLongerThan300_Throws()
    {
        var tour = Tour.Create("Title", "Description", TourType.Beach, "Greece", 7, null);

        var act = () => tour.SetShortDescription(new string('a', TourContentLimits.ShortDescriptionMaxLength + 1));

        act.Should().Throw<CatalogDomainException>();
        tour.ShortDescription.Should().BeNull();
    }

    private static int? CoverWidthFor(string brokenCode) =>
        brokenCode == TourPublishRequirementCodes.ImagesCoverMinWidth
            ? TourContentLimits.CoverMinWidthPx - 1
            : TourContentLimits.CoverMinWidthPx;

    private static Tour TourMissingOneRequirement(string code)
    {
        var tour = PublishableTourFactory.Create(coverWidthPx: TourContentLimits.CoverMinWidthPx);
        switch (code)
        {
            case TourPublishRequirementCodes.Title:
                tour.SetTitle(" ");
                break;
            case TourPublishRequirementCodes.ShortDescription:
                tour.SetShortDescription(null);
                break;
            case TourPublishRequirementCodes.Description:
                tour.SetDescription(" ");
                break;
            case TourPublishRequirementCodes.ProgramDayCount:
                tour.ReplaceDays([TourDay.Create(tour.Id, 1, "Only day", "Too short a program")]);
                break;
            case TourPublishRequirementCodes.InclusionsIncluded:
                tour.ReplaceInclusions(
                    [TourInclusion.Create(tour.Id, "Visa", TourInclusionKind.NotIncluded)]);
                break;
            case TourPublishRequirementCodes.MealPlan:
                tour.SetConditions(null, "Hotel 4*");
                break;
            case TourPublishRequirementCodes.Accommodation:
                tour.SetConditions(MealPlan.BB, " ");
                break;
            case TourPublishRequirementCodes.OffersFuture:
                var past = Now.AddDays(-20);
                tour.ReplaceOffers([TourOffer.Create(tour.Id, past, past.AddDays(5), 900m, "EUR", 4)]);
                break;
            case TourPublishRequirementCodes.ImagesCover:
                tour.ReplaceImages(
                [
                    TourImage.Create(tour.Id, Guid.NewGuid(), 0, false, "One"),
                    TourImage.Create(tour.Id, Guid.NewGuid(), 1, false, "Two"),
                    TourImage.Create(tour.Id, Guid.NewGuid(), 2, false, "Three")
                ], TourContentLimits.CoverMinWidthPx);
                break;
            case TourPublishRequirementCodes.ImagesMinCount:
                tour.ReplaceImages(
                [
                    TourImage.Create(tour.Id, Guid.NewGuid(), 0, true, "Cover", TourContentLimits.CoverMinWidthPx),
                    TourImage.Create(tour.Id, Guid.NewGuid(), 1, false, "Second")
                ]);
                break;
            case TourPublishRequirementCodes.ImagesCoverMinWidth:
                break;
            default:
                throw new InvalidOperationException(code);
        }

        return tour;
    }
}
