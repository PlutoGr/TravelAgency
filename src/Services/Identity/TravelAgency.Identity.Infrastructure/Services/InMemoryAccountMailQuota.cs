using System.Collections.Concurrent;
using TravelAgency.Identity.Application.Abstractions;

namespace TravelAgency.Identity.Infrastructure.Services;

public sealed class InMemoryAccountMailQuota : IAccountMailQuota
{
    private const int Limit = 3;
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);
    private readonly ConcurrentDictionary<string, Slot> _slots = new();

    public Task<AccountMailSlot> TryReserveAsync(string bucket, string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var slot = _slots.GetOrAdd(Key(bucket, normalizedEmail), _ => new Slot());
        lock (slot.Gate)
        {
            var now = DateTime.UtcNow;
            slot.Hits.RemoveAll(hit => hit <= now - Window);
            if (slot.Hits.Count >= Limit)
                return Task.FromResult(AccountMailSlot.Exhausted);

            slot.Hits.Add(now);
            return Task.FromResult(AccountMailSlot.Reserved);
        }
    }

    public Task ReleaseAsync(string bucket, string normalizedEmail, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(Key(bucket, normalizedEmail), out var slot))
            return Task.CompletedTask;

        lock (slot.Gate)
        {
            if (slot.Hits.Count > 0)
                slot.Hits.RemoveAt(slot.Hits.Count - 1);
            if (slot.Hits.Count == 0)
                _slots.TryRemove(Key(bucket, normalizedEmail), out _);
        }

        return Task.CompletedTask;
    }

    public static string Key(string bucket, string normalizedEmail) =>
        $"account-mail:{bucket}:{normalizedEmail}";

    private sealed class Slot
    {
        public object Gate { get; } = new();
        public List<DateTime> Hits { get; } = [];
    }
}
