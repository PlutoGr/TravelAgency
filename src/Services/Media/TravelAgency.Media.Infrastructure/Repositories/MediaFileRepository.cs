using Microsoft.EntityFrameworkCore;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.Persistence;

namespace TravelAgency.Media.Infrastructure.Repositories;

/// <summary>
/// EF Core SQLite repository for media file metadata.
/// </summary>
public sealed class MediaFileRepository : IMediaFileRepository
{
    private readonly MediaDbContext _db;

    public MediaFileRepository(MediaDbContext db)
    {
        _db = db;
    }

    public async Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.MediaFiles
            .Include(f => f.Thumbnails)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task AddAsync(MediaFile file, CancellationToken ct = default)
    {
        _db.MediaFiles.Add(file);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
