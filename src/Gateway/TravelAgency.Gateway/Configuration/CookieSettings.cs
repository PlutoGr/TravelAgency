namespace TravelAgency.Gateway.Configuration;

/// <summary>
/// Configuration for auth token cookies set by the Gateway on login/register/refresh responses.
/// </summary>
public sealed class CookieSettings
{
    public const string SectionName = "Cookie";

    /// <summary>
    /// Whether the Secure flag is set on cookies. Should be true in production.
    /// </summary>
    public bool Secure { get; set; } = true;

    /// <summary>
    /// Cookie path. Use "/" or "/api/v1" for API-scoped cookies.
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// SameSite policy. Lax is recommended for auth cookies.
    /// </summary>
    public string SameSite { get; set; } = "Lax";

    /// <summary>
    /// Optional cookie domain. When set (e.g. ".travelagency.com"), cookies are shared across subdomains.
    /// Leave null/empty for default (current host). Override via Cookie__Domain env var in production.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Cookie name for the access token.
    /// </summary>
    public string AccessTokenName { get; set; } = "access_token";

    /// <summary>
    /// Cookie name for the refresh token.
    /// </summary>
    public string RefreshTokenName { get; set; } = "refresh_token";
}
