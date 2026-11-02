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
    /// Подтверждение почты: в тело дописывается refreshToken из cookie.
    /// </summary>
    public string EmailConfirmationPath { get; set; } = "/api/v1/auth/email-confirmations";

    public string PasswordResetRequestPath { get; set; } = "/api/v1/auth/password-resets/request";

    /// <summary>
    /// Новые пути среза, которые делят предел auth со входом. Сравнение по префиксу без учёта регистра.
    /// </summary>
    public string[] AuthLimitPaths { get; set; } =
    [
        "/api/v1/auth/password-resets",
        "/api/v1/auth/me/password",
        "/api/v1/auth/me/email",
        "/api/v1/auth/email-confirmations",
        "/api/v1/auth/email-cancellations"
    ];

    /// <summary>
    /// Paths that return tokens on success (login, register, refresh, смена пароля в настройках).
    /// Compared with HttpContext.Request.Path after ASP.NET decoded percent-encoding,
    /// dropping "." segments and a trailing slash, without regard to case.
    /// </summary>
    public string[] TokenResponsePaths { get; set; } =
        ["/api/v1/auth/login", "/api/v1/auth/register", "/api/v1/auth/refresh", "/api/v1/auth/me/password"];

    public bool IsAuthRateLimited(string path)
    {
        if (TokenResponsePaths.Any(item => path.StartsWith(item.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
            return true;
        if (path.Equals(LogoutPath, StringComparison.OrdinalIgnoreCase))
            return true;
        return AuthLimitPaths.Any(item => path.StartsWith(item.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPasswordResetRequest(string path) =>
        path.TrimEnd('/').Equals(PasswordResetRequestPath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}
