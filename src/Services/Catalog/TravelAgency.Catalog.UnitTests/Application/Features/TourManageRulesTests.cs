using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Features.Tours.Manage;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;
using TravelAgency.Catalog.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class TourManageRulesTests
{
    private readonly Mock<ITourRepository> _tours = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IMediaFilesClient> _media = new();
    private readonly TourManageStore _store;

    public TourManageRulesTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _store = new TourManageStore(_tours.Object, _unitOfWork.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Basics_WhenManagerEditsForeignTour_ThrowsForbidden()
    {
        var owner = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        As(stranger, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);

        var act = () => new UpdateTourBasicsCommandHandler(_store).Handle(
            new UpdateTourBasicsCommand(tour.Id, TourEtag.Format(tour.Version), Basics("Чужой")),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Basics_WhenManagerEditsOwnDraft_Saves()
    {
        var owner = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);

        var result = await new UpdateTourBasicsCommandHandler(_store).Handle(
            new UpdateTourBasicsCommand(tour.Id, TourEtag.Format(tour.Version), Basics("Свои")),
            CancellationToken.None);

        result.Title.Should().Be("Свои");
        result.Source.Should().Be(nameof(TourSource.Manager));
        result.OwnerId.Should().Be(owner);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Basics_WithoutIfMatch_ThrowsPreconditionRequired()
    {
        var owner = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);

        var act = () => new UpdateTourBasicsCommandHandler(_store).Handle(
            new UpdateTourBasicsCommand(tour.Id, null, Basics("Без версии")),
            CancellationToken.None);

        await act.Should().ThrowAsync<PreconditionRequiredException>();
    }

    [Fact]
    public async Task Basics_WithStaleIfMatch_ThrowsConflict()
    {
        var owner = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);

        var act = () => new UpdateTourBasicsCommandHandler(_store).Handle(
            new UpdateTourBasicsCommand(tour.Id, "\"999\"", Basics("Старая версия")),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Unpublish_WhenAdminUnpublishesForeignTour_Succeeds()
    {
        var owner = Guid.NewGuid();
        var tour = OwnedPublished(owner);
        As(Guid.NewGuid(), AppRoles.Admin);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);

        var result = await new UnpublishTourCommandHandler(_store).Handle(
            new UnpublishTourCommand(tour.Id, TourEtag.Format(tour.Version)),
            CancellationToken.None);

        result.Status.Should().Be(nameof(TourStatus.Unpublished));
    }

    [Fact]
    public async Task Unpublish_WhenManagerUnpublishesForeignTour_ThrowsForbidden()
    {
        var tour = OwnedPublished(Guid.NewGuid());
        As(Guid.NewGuid(), AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);

        var act = () => new UnpublishTourCommandHandler(_store).Handle(
            new UnpublishTourCommand(tour.Id, TourEtag.Format(tour.Version)),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        tour.Status.Should().Be(TourStatus.Published);
    }

    [Fact]
    public async Task Delete_WhenTourIsNotDraft_ThrowsConflict()
    {
        var owner = Guid.NewGuid();
        var tour = OwnedPublished(owner);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);

        var act = () => new DeleteManagedTourCommandHandler(_store).Handle(
            new DeleteManagedTourCommand(tour.Id, TourEtag.Format(tour.Version)),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _tours.Verify(t => t.Remove(It.IsAny<Tour>()), Times.Never);
    }

    [Fact]
    public async Task Images_WhenMediaOmitsId_Throws422()
    {
        var owner = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        var missing = Guid.NewGuid();
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        _media.Setup(m => m.GetMediaFilesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var act = () => ImagesHandler().Handle(ImagesCommand(tour, missing, isCover: false, widthIgnored: true), CancellationToken.None);

        var error = await act.Should().ThrowAsync<TourImageRuleException>();
        error.Which.Code.Should().Be(TourImageRules.NotFound);
    }

    [Fact]
    public async Task Images_WhenFileOwnerIsNotTourManager_Throws422()
    {
        var owner = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        var fileId = Guid.NewGuid();
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        ReturnFiles(new RemoteMediaFile(fileId, Guid.NewGuid().ToString(), 2000, 1000));

        var act = () => ImagesHandler().Handle(ImagesCommand(tour, fileId, isCover: false), CancellationToken.None);

        var error = await act.Should().ThrowAsync<TourImageRuleException>();
        error.Which.Code.Should().Be(TourImageRules.OwnerMismatch);
        tour.Images.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1279)]
    public async Task Images_WhenCoverWidthIsUnknownOrBelow1280_ThrowsCoverMinWidth(int width)
    {
        var owner = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        var fileId = Guid.NewGuid();
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        ReturnFiles(new RemoteMediaFile(fileId, owner.ToString(), width, 800));

        var act = () => ImagesHandler().Handle(ImagesCommand(tour, fileId, isCover: true), CancellationToken.None);

        var error = await act.Should().ThrowAsync<TourImageRuleException>();
        error.Which.Code.Should().Be(TourPublishRequirementCodes.ImagesCoverMinWidth);
    }

    [Fact]
    public async Task Images_WhenNonCoverIsNarrow_AcceptsIt()
    {
        var owner = Guid.NewGuid();
        var tour = Tour.CreateDraft(owner);
        var coverId = Guid.NewGuid();
        var narrowId = Guid.NewGuid();
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        ReturnFiles(
            new RemoteMediaFile(coverId, owner.ToString(), 1280, 800),
            new RemoteMediaFile(narrowId, owner.ToString(), 100, 80));

        var command = new UpdateTourImagesCommand(
            tour.Id,
            TourEtag.Format(tour.Version),
            new UpdateTourImagesRequest(
            [
                new TourImageInput(coverId, 0, true, "cover"),
                new TourImageInput(narrowId, 1, false, "narrow")
            ]));

        var result = await ImagesHandler().Handle(command, CancellationToken.None);

        result.Images.Should().HaveCount(2);
        result.Images.Single(i => !i.IsCover).WidthPx.Should().Be(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1279)]
    public async Task Publish_WhenCoverWidthFromMediaIsUnknownOrBelow1280_ThrowsAndStaysDraft(int width)
    {
        var owner = Guid.NewGuid();
        var tour = OwnedReady(owner, imageCount: 3);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        var files = tour.Images
            .Select(image => new RemoteMediaFile(
                image.MediaFileId,
                owner.ToString(),
                image.IsCover ? width : 800,
                600))
            .ToArray();
        ReturnFiles(files);

        var act = () => new PublishTourCommandHandler(_store, _media.Object).Handle(
            new PublishTourCommand(tour.Id, TourEtag.Format(tour.Version)),
            CancellationToken.None);

        var error = await act.Should().ThrowAsync<TourImageRuleException>();
        error.Which.Code.Should().Be(TourPublishRequirementCodes.ImagesCoverMinWidth);
        tour.Status.Should().Be(TourStatus.Draft);
        _media.Verify(m => m.MarkPublicAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_WhenFewerThanThreePhotos_ThrowsWithMissingCode_AndDoesNotMarkPublic()
    {
        var owner = Guid.NewGuid();
        var tour = OwnedReady(owner, imageCount: 2);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        ReturnOwned(owner, tour);

        var act = () => new PublishTourCommandHandler(_store, _media.Object).Handle(
            new PublishTourCommand(tour.Id, TourEtag.Format(tour.Version)),
            CancellationToken.None);

        var error = await act.Should().ThrowAsync<TourNotPublishableException>();
        error.Which.Missing.Should().Contain(TourPublishRequirementCodes.ImagesMinCount);
        tour.Status.Should().Be(TourStatus.Draft);
        _media.Verify(m => m.MarkPublicAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_WhenMarkPublicFails_KeepsDraftAndDoesNotSave()
    {
        var owner = Guid.NewGuid();
        var tour = OwnedReady(owner, imageCount: 3);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        ReturnOwned(owner, tour);
        _media.Setup(m => m.MarkPublicAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MediaUnavailableException());

        var act = () => new PublishTourCommandHandler(_store, _media.Object).Handle(
            new PublishTourCommand(tour.Id, TourEtag.Format(tour.Version)),
            CancellationToken.None);

        await act.Should().ThrowAsync<MediaUnavailableException>();
        tour.Status.Should().Be(TourStatus.Draft);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_WhenMediaIsDown_ThrowsAndKeepsDraft()
    {
        var owner = Guid.NewGuid();
        var tour = OwnedReady(owner, imageCount: 3);
        As(owner, AppRoles.Manager);
        _tours.Setup(t => t.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        _media.Setup(m => m.GetMediaFilesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MediaUnavailableException());

        var act = () => new PublishTourCommandHandler(_store, _media.Object).Handle(
            new PublishTourCommand(tour.Id, TourEtag.Format(tour.Version)),
            CancellationToken.None);

        await act.Should().ThrowAsync<MediaUnavailableException>();
        tour.Status.Should().Be(TourStatus.Draft);
    }

    [Fact]
    public async Task Create_SetsSourceManagerAndOwner()
    {
        var owner = Guid.NewGuid();
        As(owner, AppRoles.Manager);
        Tour? saved = null;
        _tours.Setup(t => t.AddAsync(It.IsAny<Tour>(), It.IsAny<CancellationToken>()))
            .Callback<Tour, CancellationToken>((tour, _) => saved = tour)
            .Returns(Task.CompletedTask);

        var result = await new CreateTourDraftCommandHandler(_store).Handle(
            new CreateTourDraftCommand(new CreateTourDraftRequest("Черновик", null, null, null, null, null, null, null)),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Source.Should().Be(TourSource.Manager);
        saved.OwnerId.Should().Be(owner);
        saved.Status.Should().Be(TourStatus.Draft);
        result.Source.Should().Be(nameof(TourSource.Manager));
    }

    [Fact]
    public void BasicsValidator_RejectsShortDescriptionLongerThan300()
    {
        var validator = new UpdateTourBasicsCommandValidator();
        var command = new UpdateTourBasicsCommand(
            Guid.NewGuid(),
            "\"1\"",
            Basics(new string('a', TourContentLimits.ShortDescriptionMaxLength + 1)));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PricesValidator_RejectsNonPositivePrice()
    {
        var validator = new UpdateTourManagePricesCommandValidator();
        var command = new UpdateTourManagePricesCommand(
            Guid.NewGuid(),
            "\"1\"",
            new UpdateTourManagePricesRequest(
            [
                new TourOfferInput(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(8), 0, "EUR", 4)
            ]));

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    private UpdateTourImagesCommandHandler ImagesHandler() => new(_store, _media.Object);

    private static UpdateTourImagesCommand ImagesCommand(Tour tour, Guid fileId, bool isCover, bool widthIgnored = false) =>
        new(
            tour.Id,
            TourEtag.Format(tour.Version),
            new UpdateTourImagesRequest([new TourImageInput(fileId, 0, isCover, widthIgnored ? null : "alt")]));

    private void ReturnFiles(params RemoteMediaFile[] files)
    {
        _media.Setup(m => m.GetMediaFilesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);
    }

    private void ReturnOwned(Guid owner, Tour tour)
    {
        var files = tour.Images
            .Select(image => new RemoteMediaFile(
                image.MediaFileId,
                owner.ToString(),
                image.IsCover ? TourContentLimits.CoverMinWidthPx : 800,
                600))
            .ToArray();
        ReturnFiles(files);
    }

    private void As(Guid userId, string role)
    {
        _currentUser.Setup(u => u.UserId).Returns(userId);
        _currentUser.Setup(u => u.Role).Returns(role);
        _currentUser.Setup(u => u.IsAuthenticated).Returns(true);
    }

    private static UpdateTourBasicsRequest Basics(string title) =>
        new(title, "Кратко", "Москва", "Греция", nameof(TourType.Beach), 7, null);

    private static Tour OwnedPublished(Guid owner)
    {
        var tour = OwnedReady(owner, imageCount: 3);
        tour.Publish(DateTime.UtcNow, TourContentLimits.CoverMinWidthPx);
        return tour;
    }

    private static Tour OwnedReady(Guid owner, int imageCount)
    {
        const int days = 2;
        var tour = Tour.CreateDraft(owner);
        tour.SetBasics("Полный тур", "Кратко", "Москва", "Греция", TourType.Beach, days, null);
        tour.SetDescription("Полное описание тура для публикации");
        tour.ReplaceDays(Enumerable.Range(1, days)
            .Select(day => TourDay.Create(tour.Id, day, $"День {day}", "Программа")));
        tour.ReplaceConditions(
            [TourInclusion.Create(tour.Id, "Перелёт", TourInclusionKind.Included)],
            MealPlan.BB,
            "Отель");
        var from = DateTime.UtcNow.AddDays(10);
        tour.ReplaceOffers([TourOffer.Create(tour.Id, from, from.AddDays(days), 1000m, "EUR", 8)]);
        var images = Enumerable.Range(0, imageCount)
            .Select(index => TourImage.Create(
                tour.Id,
                Guid.NewGuid(),
                index,
                index == 0,
                $"Фото {index}",
                index == 0 ? TourContentLimits.CoverMinWidthPx : 800))
            .ToList();
        tour.ReplaceImages(images, TourContentLimits.CoverMinWidthPx);
        return tour;
    }
}
