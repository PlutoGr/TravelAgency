using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Features.Presign;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.UnitTests.Application.Features;

public class PresignMediaQueryHandlerTests
{
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
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "user/key/photo.jpg", "user-1");
        var expectedUrl = "https://storage/photo.jpg?X-Amz-Signature=abc123";

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(
                mediaFile.StorageKey,
                TimeSpan.FromMinutes(30),
                Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        var result = await _handler.Handle(new PresignMediaQuery(fileId), CancellationToken.None);

        result.Url.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task Handle_ExistingActiveFile_ReturnsCorrectExpiryTime()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user-1");
        var beforeCall = DateTimeOffset.UtcNow;

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://url");

        var result = await _handler.Handle(new PresignMediaQuery(fileId), CancellationToken.None);

        var expectedExpiry = beforeCall.AddMinutes(30);
        result.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ExistingActiveFile_UsesConfiguredTtl()
    {
        var fileId = Guid.NewGuid();
        var mediaFile = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user-1");
        TimeSpan? capturedTtl = null;

        _repository.GetByIdAsync(fileId, Arg.Any<CancellationToken>()).Returns(mediaFile);
        _storage.GeneratePresignedUrlAsync(
                Arg.Any<string>(),
                Arg.Do<TimeSpan>(ttl => capturedTtl = ttl),
                Arg.Any<CancellationToken>())
            .Returns("https://url");

        await _handler.Handle(new PresignMediaQuery(fileId), CancellationToken.None);

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
}
