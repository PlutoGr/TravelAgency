using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;

namespace TravelAgency.Identity.Infrastructure.Services;

/// <summary>
/// In-memory implementation of account lockout. State is lost on application restart.
/// </summary>
public sealed class InMemoryLockoutService : ILockoutService
{
    private readonly LockoutSettings _settings;
    private readonly ConcurrentDictionary<string, LockoutEntry> _entries = new();

    public InMemoryLockoutService(IOptions<LockoutSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsLockedOut(string email)
    {
        var key = NormalizeEmail(email);
        if (!_entries.TryGetValue(key, out var entry))
            return false;

        if (entry.LockoutUntil.HasValue && DateTime.UtcNow < entry.LockoutUntil.Value)
            return true;

        return false;
    }

    public void RecordFailedAttempt(string email)
    {
        var key = NormalizeEmail(email);
        var now = DateTime.UtcNow;

        _entries.AddOrUpdate(
            key,
            _ => CreateNewEntry(now, 1),
            (_, existing) => UpdateEntry(existing, now));
    }

    public void ResetFailedAttempts(string email)
    {
        var key = NormalizeEmail(email);
        _entries.TryRemove(key, out _);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private LockoutEntry CreateNewEntry(DateTime now, int count)
    {
        var lockoutUntil = count >= _settings.LockoutThreshold
            ? now.AddMinutes(_settings.LockoutDurationMinutes)
            : (DateTime?)null;

        return new LockoutEntry(count, lockoutUntil);
    }

    private LockoutEntry UpdateEntry(LockoutEntry existing, DateTime now)
    {
        if (existing.LockoutUntil.HasValue && now < existing.LockoutUntil.Value)
            return existing;

        var newCount = existing.LockoutUntil.HasValue ? 1 : existing.FailedAttemptCount + 1;
        return CreateNewEntry(now, newCount);
    }

    private sealed record LockoutEntry(int FailedAttemptCount, DateTime? LockoutUntil);
}
