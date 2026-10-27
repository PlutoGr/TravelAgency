namespace TravelAgency.Identity.Application.Abstractions;

public sealed record AvatarUploadResult(Guid MediaFileId);

/// <summary>
/// Uploads and deletes avatar objects via the Media service (HTTP), keeping MinIO URLs internal.
/// </summary>
public interface IAvatarMediaClient
{
    Task<AvatarUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string? bearerToken,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid mediaFileId, string? bearerToken, CancellationToken cancellationToken = default);
}
