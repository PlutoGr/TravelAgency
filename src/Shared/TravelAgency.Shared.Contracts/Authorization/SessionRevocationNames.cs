using System.Globalization;
using System.Security.Claims;

namespace TravelAgency.Shared.Contracts.Authorization;

/// <summary>
/// Версия сессии в access-токене и запись в Redis, по которой шлюз и чат
/// отклоняют токен после блокировки или понижения роли.
/// </summary>
public static class SessionRevocationNames
{
    public const string ClaimType = "sv";
    public const string Channel = "identity:session-revoked";

    public static string Key(Guid userId) => $"identity:session:{userId:N}";

    public static string Format(long version, bool blocked) =>
        string.Create(CultureInfo.InvariantCulture, $"{version}|{(blocked ? 1 : 0)}");

    public static bool TryParse(string? value, out long version, out bool blocked)
    {
        version = 0;
        blocked = false;
        if (string.IsNullOrEmpty(value))
            return false;

        var separator = value.IndexOf('|');
        if (separator <= 0 || separator != value.LastIndexOf('|'))
            return false;

        if (!long.TryParse(value.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out version))
            return false;

        var flag = value[(separator + 1)..];
        if (flag is not ("0" or "1"))
            return false;

        blocked = flag == "1";
        return true;
    }

    public static long ReadVersion(ClaimsPrincipal? user)
    {
        var raw = user?.FindFirst(ClaimType)?.Value;
        return long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var version) ? version : 0;
    }

    public static bool Rejects(SessionRevocationEntry? entry, long tokenVersion) =>
        entry is not null && (entry.Blocked || entry.Version != tokenVersion);
}

public sealed record SessionRevocationEntry(long Version, bool Blocked);
