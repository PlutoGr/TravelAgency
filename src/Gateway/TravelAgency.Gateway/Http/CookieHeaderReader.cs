namespace TravelAgency.Gateway.Http;

/// <summary>
/// Reads the raw Cookie header. The header has names and values, not Path.
/// RFC 6265 section 5.4 sends a cookie with a longer path before one with a
/// shorter path, so the first value of a name is the most specific cookie.
/// Request.Cookies keeps one value per name and the later one overwrites the
/// earlier one, so the shorter path would win. The value is still decoded with
/// Uri.UnescapeDataString, the same call Request.Cookies makes after
/// Response.Cookies.Append encoded it with Uri.EscapeDataString.
/// A broken percent-sequence does not fail the request.
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
                if (value.Length == 0)
                    return null;

                return Unescape(value.ToString());
            }

            if (semi < 0)
                break;
            start = semi + 1;
        }

        return null;
    }

    /// <summary>
    /// Same decoder as Request.Cookies. UriFormatException means the cookie is missing.
    /// </summary>
    private static string? Unescape(string value)
    {
        try
        {
            var decoded = Uri.UnescapeDataString(value);
            return decoded.Length == 0 ? null : decoded;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }
}
