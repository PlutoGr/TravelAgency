using System.Collections.Concurrent;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory repository for MVP. Replace with EF Core + PostgreSQL in V2.
/// </summary>
public sealed class InMemoryMediaFileRepository : IMediaFileRepository
{
    private readonly ConcurrentDictionary<Guid, MediaFile> _store = new();

    public Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var file) ? file : null);

    public Task AddAsync(MediaFile file, CancellationToken ct = default)
    {
        _store[file.Id] = file;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        Task.CompletedTask;
}
