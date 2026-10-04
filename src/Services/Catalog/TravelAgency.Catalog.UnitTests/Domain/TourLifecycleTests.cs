using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.UnitTests.Domain;

public class TourLifecycleTests
{
    [Fact]
    public void Delete_WhenDraft_DoesNotThrow()
    {
        var tour = Tour.Create("Draft", "Description", TourType.Beach, "Greece", 7, null);

        var act = () => tour.Delete();

        act.Should().NotThrow();
        tour.Status.Should().Be(TourStatus.Draft);
    }

    [Fact]
    public void Delete_WhenPublished_ThrowsAndStaysPublished()
    {
        var tour = PublishableTourFactory.Create();
        tour.Publish(DateTime.UtcNow, TourContentLimits.CoverMinWidthPx);

        var act = () => tour.Delete();

        act.Should().Throw<CatalogDomainException>();
        tour.Status.Should().Be(TourStatus.Published);
    }

    [Fact]
    public void Delete_WhenUnpublished_Throws()
    {
        var tour = PublishableTourFactory.Create();
        tour.Publish(DateTime.UtcNow, TourContentLimits.CoverMinWidthPx);
        tour.Unpublish();

        var act = () => tour.Delete();

        act.Should().Throw<CatalogDomainException>();
        tour.Status.Should().Be(TourStatus.Unpublished);
        tour.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Update_WhenPublishedEditBreaksDayCount_IsRejectedAndTourStaysPublished()
    {
        var tour = PublishableTourFactory.Create(durationDays: 2);
        tour.Publish(DateTime.UtcNow, TourContentLimits.CoverMinWidthPx);

        var act = () => tour.Update(
            tour.Title,
            tour.Description,
            tour.TourType,
            tour.Country,
            durationDays: 5,
            tour.ImageUrl,
            tour.DirectionId);

        act.Should().Throw<TourNotPublishableException>()
            .Which.Missing.Should().Contain(TourPublishRequirementCodes.ProgramDayCount);
        tour.Status.Should().Be(TourStatus.Published);
        tour.DurationDays.Should().Be(2);
        tour.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ReplaceImages_WhenTwoCovers_ThrowsAndKeepsPreviousImages()
    {
        var tour = PublishableTourFactory.Create();
        var previousCount = tour.Images.Count;

        var act = () => tour.ReplaceImages(
        [
            TourImage.Create(tour.Id, Guid.NewGuid(), 0, true, "First cover"),
            TourImage.Create(tour.Id, Guid.NewGuid(), 1, true, "Second cover"),
            TourImage.Create(tour.Id, Guid.NewGuid(), 2, false, "Other")
        ]);

        act.Should().Throw<CatalogDomainException>();
        tour.Images.Should().HaveCount(previousCount);
        tour.Images.Count(i => i.IsCover).Should().Be(1);
    }

    [Fact]
    public void ReplaceImages_WhenMoreThan20_Throws()
    {
        var tour = Tour.Create("Title", "Description", TourType.Beach, "Greece", 7, null);
        var images = Enumerable.Range(0, TourContentLimits.MaxImagesPerTour + 1)
            .Select(i => TourImage.Create(tour.Id, Guid.NewGuid(), i, isCover: i == 0))
            .ToList();

        var act = () => tour.ReplaceImages(images);

        act.Should().Throw<CatalogDomainException>();
        tour.Images.Should().BeEmpty();
    }

    [Fact]
    public void Unpublish_AfterPublish_HidesTourWithoutDeletingIt()
    {
        var tour = PublishableTourFactory.Create();
        tour.Publish(DateTime.UtcNow, TourContentLimits.CoverMinWidthPx);

        tour.Unpublish();

        tour.Status.Should().Be(TourStatus.Unpublished);
        tour.IsActive.Should().BeFalse();
        tour.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public void PendingReview_IsReservedAndUnused()
    {
        Enum.IsDefined(TourStatus.PendingReview).Should().BeTrue();
        var tour = Tour.Create("Title", "Description", TourType.Beach, "Greece", 7, null);
        tour.Status.Should().NotBe(TourStatus.PendingReview);
    }
}
