using TravelAgency.Identity.Application.Abstractions;

namespace TravelAgency.Identity.IntegrationTests.Fakes;

public sealed class FakeAvatarMediaClient : IAvatarMediaClient
{
    public Guid NextId { get; set; } = Guid.NewGuid();
    public List<Guid> Uploaded { get; } = [];
    public List<Guid> Deleted { get; } = [];
    public Exception? UploadError { get; set; }
    public Exception? DeleteError { get; set; }

    public Task<AvatarUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (UploadError is not null)
            throw UploadError;

        Uploaded.Add(NextId);
        return Task.FromResult(new AvatarUploadResult(NextId));
    }

    public Task DeleteAsync(Guid mediaFileId, string? bearerToken, CancellationToken cancellationToken = default)
    {
        if (DeleteError is not null)
            throw DeleteError;

        Deleted.Add(mediaFileId);
        return Task.CompletedTask;
    }
}
