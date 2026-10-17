using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Features.Presign;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class PresignMediaQueryHandlerTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly IMediaFileRepository _repository = Substitute.For<IMediaFileRepository>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private readonly StorageSettings _storageSettings = new()
    {
        PresignTtlMinutes = 30
    };

    private readonly PresignMediaQueryHandler _handler;

    public PresignMediaQueryHandlerTests()
    {
        _handler = new PresignMediaQueryHandler(_repository, _storage, Options.Create(_storageSettings));
    }

    [Fact]
    public async Task Handle_ExistingActiveFile_ReturnsPresignedUrl()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "user/key/photo.jpg", OwnerId.ToString());
        var expectedUrl = "https://storage/photo.jpg?X-Amz-Signature=abc123";

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(
                mediaFile.StorageKey,
                TimeSpan.FromMinutes(30),
                Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        var result = await _handler.Handle(AsOwner(fileId), CancellationToken.None);

        result.Url.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsPresignedUrl()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = PrivateGeneral();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://owner");

        var result = await _handler.Handle(AsOwner(fileId), CancellationToken.None);

        result.Url.Should().Be("https://owner");
    }

    [Fact]
    public async Task Handle_OtherClient_PublicGeneral_ThrowsNotFound()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = PrivateGeneral();
        mediaFile.MarkPublic();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        var act = async () => await _handler.Handle(As(fileId, OtherId, AppRoles.Client), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
        await _storage.DidNotReceive()
            .GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OtherClient_PublicTourImage_ReturnsPresignedUrl()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create(
            "cover.jpg", "image/jpeg", 20, "owners/cover.jpg", OwnerId.ToString(), MediaPurposes.TourImage);
        mediaFile.MarkPublic();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://tour");

        var result = await _handler.Handle(As(fileId, OtherId, AppRoles.Client), CancellationToken.None);

        result.Url.Should().Be("https://tour");
    }

    [Fact]
    public async Task Handle_Manager_OtherPrivateFile_ReturnsPresignedUrl()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(PrivateGeneral());
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://manager");

        var result = await _handler.Handle(As(fileId, OtherId, AppRoles.Manager), CancellationToken.None);

        result.Url.Should().Be("https://manager");
    }

    [Fact]
    public async Task Handle_ExistingActiveFile_ReturnsCorrectExpiryTime()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", OwnerId.ToString());
        var beforeCall = DateTimeOffset.UtcNow;

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://url");

        var result = await _handler.Handle(AsOwner(fileId), CancellationToken.None);

        var expectedExpiry = beforeCall.AddMinutes(30);
        result.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ExistingActiveFile_UsesConfiguredTtl()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", OwnerId.ToString());
        TimeSpan? capturedTtl = null;

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(
                Arg.Any<string>(),
                Arg.Do<TimeSpan>(ttl => capturedTtl = ttl),
                Arg.Any<CancellationToken>())
            .Returns("https://url");

        await _handler.Handle(AsOwner(fileId), CancellationToken.None);

        capturedTtl.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns((MediaFile?)null);

        var act = async () => await _handler.Handle(new PresignMediaQuery(fileId), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedFile_ThrowsMediaNotFoundException()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user-1");
        mediaFile.MarkAsDeleted();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        var act = async () => await _handler.Handle(new PresignMediaQuery(fileId), CancellationToken.None);

        await act.Should().ThrowAsync<MediaNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedFile_DoesNotGeneratePresignedUrl()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user-1");
        mediaFile.MarkAsDeleted();
        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);

        try { await _handler.Handle(new PresignMediaQuery(fileId), CancellationToken.None); } catch { }

        await _storage.DidNotReceive().GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    private static MediaFile PrivateGeneral() =>
        MediaFile.Create("draft.pdf", "application/pdf", 32, "owners/draft.pdf", OwnerId.ToString());

    private static PresignMediaQuery AsOwner(Guid fileId) => As(fileId, OwnerId, AppRoles.Client);

    private static PresignMediaQuery As(Guid fileId, Guid userId, params string[] roles) =>
        new(fileId, UserId: userId, Roles: roles);
}
