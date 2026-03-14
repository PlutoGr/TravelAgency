using TravelAgency.Media.Domain.Entities;

namespace TravelAgency.Media.Domain.Interfaces;

public interface IMediaFileRepository
{
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(MediaFile file, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
