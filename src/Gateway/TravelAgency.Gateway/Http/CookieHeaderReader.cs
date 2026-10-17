namespace TravelAgency.Gateway.Http;

/// <summary>
/// Reads the raw Cookie header. The header has names and values, not Path.
/// RFC 6265 section 5.4 sends a cookie with a longer path before one with a
/// shorter path, so the first value of a name is the most specific cookie.
/// </summary>
internal static class CookieHeaderReader
{
    public static string? ReadFirst(string? cookieHeader, string name)
    {
        if (string.IsNullOrEmpty(cookieHeader) || string.IsNullOrEmpty(name))
            return null;

        var prefix = name + "=";
        var start = 0;
        while (start < cookieHeader.Length)
        {
            while (start < cookieHeader.Length && cookieHeader[start] is ' ' or '\t')
                start++;

            var semi = cookieHeader.IndexOf(';', start);
            var end = semi < 0 ? cookieHeader.Length : semi;
            var part = cookieHeader.AsSpan(start, end - start).Trim();
            if (part.StartsWith(prefix, StringComparison.Ordinal))
            {
                var value = part[prefix.Length..].Trim();
                if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                    value = value[1..^1];
                return value.Length == 0 ? null : value.ToString();
            }

            if (semi < 0)
                break;
            start = semi + 1;
        }

        return null;
    }
}
