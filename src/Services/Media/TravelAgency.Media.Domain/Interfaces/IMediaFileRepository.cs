using TravelAgency.Media.Domain.Entities;

namespace TravelAgency.Media.Domain.Interfaces;

/// <summary>
/// Repository for media file metadata.
/// In-memory implementation: in-place mutations (e.g. MarkAsDeleted) are persisted via SaveChangesAsync.
/// Future EF Core implementation: tracked entities are persisted when SaveChangesAsync is called.
/// </summary>
public interface IMediaFileRepository
{
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MediaFile>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Sets <see cref="MediaFile.IsPublic"/> for active files. Missing ids are ignored.
    /// Already-public files stay public. Does not publish deleted files.
    /// </summary>
    Task MarkPublicAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task AddAsync(MediaFile file, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
