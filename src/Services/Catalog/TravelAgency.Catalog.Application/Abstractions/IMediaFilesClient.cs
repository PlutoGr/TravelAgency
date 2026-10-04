namespace TravelAgency.Catalog.Application.Abstractions;

public interface IMediaFilesClient
{
    Task<IReadOnlyList<RemoteMediaFile>> GetMediaFilesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task MarkPublicAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
