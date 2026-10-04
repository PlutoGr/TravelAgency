using TravelAgency.Media.Application.Features.Files;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class MediaFileReadAuthorizationTests
{
    private readonly IMediaFileRepository _repository = Substitute.For<IMediaFileRepository>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly GetPublicMediaFileQueryHandler _publicHandler;
    private readonly GetManagedMediaFileQueryHandler _managedHandler;

    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public MediaFileReadAuthorizationTests()
    {
        _storage.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream([9, 9, 9])));

        _publicHandler = new GetPublicMediaFileQueryHandler(_repository, _storage);
        _managedHandler = new GetManagedMediaFileQueryHandler(_repository, _storage, _currentUser);
    }

    [Fact]
    public async Task Public_NotYetPublic_ReturnsNotFound()
    {
        var file = TourImage(isPublic: false);
        _repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var act = () => _publicHandler.Handle(new GetPublicMediaFileQuery(file.Id, TourImageSizes.W200), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
        await _storage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Public_AfterMarkPublic_StreamsPreview()
    {
        var file = TourImage(isPublic: false);
        file.MarkPublic();
        _repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var result = await _publicHandler.Handle(
            new GetPublicMediaFileQuery(file.Id, TourImageSizes.W800), CancellationToken.None);

        result.ContentType.Should().Be("image/jpeg");
        await _storage.Received(1).DownloadAsync(
            Arg.Is<string>(key => key.EndsWith(TourImageSizes.W800)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Public_OtherPurpose_ReturnsNotFound_EvenWhenFlagIsPublic()
    {
        var file = MediaFile.Create("doc.pdf", "application/pdf", 10, "key", OwnerId.ToString());
        file.MarkPublic();
        _repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var act = () => _publicHandler.Handle(new GetPublicMediaFileQuery(file.Id, TourImageSizes.W200), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Public_UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        var act = () => _publicHandler.Handle(new GetPublicMediaFileQuery(id, TourImageSizes.W200), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Manage_Owner_CanReadPrivateFile()
    {
        var file = TourImage(isPublic: false);
        AsUser(OwnerId, AppRoles.Manager);
        _repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var result = await _managedHandler.Handle(
            new GetManagedMediaFileQuery(file.Id, TourImageSizes.W200), CancellationToken.None);

        result.Content.Should().NotBeNull();
    }

    [Fact]
    public async Task Manage_Admin_CanReadAnotherOwnersFile()
    {
        var file = TourImage(isPublic: false);
        AsUser(OtherId, AppRoles.Admin);
        _repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var result = await _managedHandler.Handle(
            new GetManagedMediaFileQuery(file.Id, TourImageSizes.W1600), CancellationToken.None);

        result.Content.Should().NotBeNull();
    }

    [Fact]
    public async Task Manage_OtherManager_IsForbidden()
    {
        var file = TourImage(isPublic: false);
        AsUser(OtherId, AppRoles.Manager);
        _repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var act = () => _managedHandler.Handle(
            new GetManagedMediaFileQuery(file.Id, TourImageSizes.W200), CancellationToken.None);

        await act.Should().ThrowAsync<MediaAccessDeniedException>();
    }

    [Fact]
    public async Task Manage_Client_IsForbidden()
    {
        var file = TourImage(isPublic: false);
        AsUser(OtherId, AppRoles.Client);
        _repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var act = () => _managedHandler.Handle(
            new GetManagedMediaFileQuery(file.Id, TourImageSizes.W200), CancellationToken.None);

        await act.Should().ThrowAsync<MediaAccessDeniedException>();
    }

    [Fact]
    public async Task Manage_Anonymous_IsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns(Guid.Empty);

        var act = () => _managedHandler.Handle(
            new GetManagedMediaFileQuery(Guid.NewGuid(), TourImageSizes.W200), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private void AsUser(Guid userId, string role)
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _currentUser.Role.Returns(role);
    }

    private static MediaFile TourImage(bool isPublic)
    {
        var file = MediaFile.Create(
            "cover.jpg", "image/jpeg", 20, "owners/cover.jpg", OwnerId.ToString(),
            MediaPurposes.TourImage, 2000, 1000);
        file.AddThumbnail("owners/cover.jpg-preview-w200", 200, 100, TourImageSizes.W200);
        file.AddThumbnail("owners/cover.jpg-preview-w800", 800, 400, TourImageSizes.W800);
        file.AddThumbnail("owners/cover.jpg-preview-w1600", 1600, 800, TourImageSizes.W1600);
        if (isPublic)
            file.MarkPublic();
        return file;
    }
}
