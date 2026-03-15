namespace TravelAgency.Gateway.Configuration;

/// <summary>
/// Configuration for auth route paths used by AuthRequestTransformProvider and AuthResponseTransformProvider.
/// Paths must match the ReverseProxy route match paths (e.g. /api/v1/auth/{**catch-all}).
/// </summary>
public sealed class AuthRouteSettings
{
    public const string SectionName = "AuthRoute";

    /// <summary>
    /// YARP route ID for auth. Must match the route in ReverseProxy:Routes.
    /// </summary>
    public string RouteId { get; set; } = "auth-route";

    /// <summary>
    /// Base path for auth API (e.g. /api/v1/auth).
    /// </summary>
    public string BasePath { get; set; } = "/api/v1/auth";

    /// <summary>
    /// Path for refresh token endpoint (relative to BasePath or full path).
    /// </summary>
    public string RefreshPath { get; set; } = "/api/v1/auth/refresh";

    /// <summary>
    /// Path for logout endpoint.
    /// </summary>
    public string LogoutPath { get; set; } = "/api/v1/auth/logout";

    /// <summary>
    /// Paths that return tokens on success (login, register, refresh).
    /// </summary>
    public string[] TokenResponsePaths { get; set; } =
        ["/api/v1/auth/login", "/api/v1/auth/register", "/api/v1/auth/refresh"];
}
