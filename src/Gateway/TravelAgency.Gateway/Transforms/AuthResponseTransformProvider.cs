using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Transforms;

/// <summary>
/// YARP transform provider that adds auth response transform for login/register/refresh and logout.
/// On 200/201 from Identity, parses tokens from JSON, sets httpOnly cookies, and strips body.
/// Both cookies expire at refreshExpiresAt from Identity. There is no local fallback lifetime.
/// The refresh cookie path is the auth prefix, so the browser sends it to refresh and logout.
/// On 204 from Identity for logout, sets clear-cookie headers (Max-Age=0) for access_token and refresh_token.
/// Login and logout also drop the legacy refresh cookie that used Path=/.
/// </summary>
internal sealed class AuthResponseTransformProvider : ITransformProvider
{
    private const string LegacyRefreshCookiePath = "/";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly CookieSettings _cookieSettings;
    private readonly AuthRouteSettings _authRouteSettings;
    private readonly ILogger<AuthResponseTransformProvider> _logger;

    public AuthResponseTransformProvider(
        IOptions<CookieSettings> cookieSettings,
        IOptions<AuthRouteSettings> authRouteSettings,
        ILogger<AuthResponseTransformProvider> logger)
    {
        _cookieSettings = cookieSettings.Value;
        _authRouteSettings = authRouteSettings.Value;
        _logger = logger;
    }

    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        if (!string.Equals(context.Route.RouteId, _authRouteSettings.RouteId, StringComparison.Ordinal))
            return;

        context.AddResponseTransform(ApplyAuthResponseTransform);
    }

    private async ValueTask ApplyAuthResponseTransform(ResponseTransformContext transformContext)
    {
        var status = transformContext.ProxyResponse?.StatusCode;
        var path = transformContext.HttpContext.Request.Path.Value ?? "";

        // Logout: on 204 from Identity, clear cookies with Max-Age=0
        if (path.Equals(_authRouteSettings.LogoutPath, StringComparison.OrdinalIgnoreCase) &&
            status == System.Net.HttpStatusCode.NoContent)
        {
            AppendClearCookies(transformContext.HttpContext.Response);
            transformContext.SuppressResponseBody = true;
            return;
        }

        if (status is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created))
            return;
        if (!_authRouteSettings.TokenResponsePaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
            return;

        var content = transformContext.ProxyResponse!.Content;
        if (content is null)
            return;

        string? body;
        await using (var stream = await content.ReadAsStreamAsync(transformContext.CancellationToken))
        {
            using var reader = new StreamReader(stream);
            body = await reader.ReadToEndAsync(transformContext.CancellationToken);
        }

        if (string.IsNullOrWhiteSpace(body))
            return;

        AuthTokens? tokens;
        try
        {
            tokens = JsonSerializer.Deserialize<AuthTokens>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize auth response JSON; passing through body");
            await PassThroughBodyAsync(transformContext, body, content);
            return;
        }

        if (tokens?.AccessToken is null || tokens.RefreshToken is null)
        {
            await PassThroughBodyAsync(transformContext, body, content);
            return;
        }

        if (tokens.RefreshExpiresAt is null)
        {
            _logger.LogWarning("Auth response has no refreshExpiresAt; cookies were not set");
            await PassThroughBodyAsync(transformContext, body, content);
            return;
        }

        var response = transformContext.HttpContext.Response;
        var expires = ToCookieExpires(tokens.RefreshExpiresAt.Value);
        response.Cookies.Append(
            _cookieSettings.AccessTokenName,
            tokens.AccessToken,
            CreateCookieOptions(_cookieSettings.Path, expires, maxAge: null));
        response.Cookies.Append(
            _cookieSettings.RefreshTokenName,
            tokens.RefreshToken,
            CreateCookieOptions(RefreshCookiePath, expires, maxAge: null));

        if (IsLoginPath(path))
            AppendLegacyRefreshCookieClear(response);

        transformContext.SuppressResponseBody = true;

        var successBody = JsonSerializer.Serialize(new { success = true });
        response.ContentType = "application/json";
        response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(successBody);
        await response.WriteAsync(successBody, transformContext.CancellationToken);
    }

    /// <summary>
    /// Writes the buffered body to the response when we consumed the stream but did not transform.
    /// Prevents the client from receiving an empty body when parse fails or tokens are missing.
    /// </summary>
    private static async ValueTask PassThroughBodyAsync(
        ResponseTransformContext transformContext,
        string body,
        HttpContent content)
    {
        transformContext.SuppressResponseBody = true;
        var response = transformContext.HttpContext.Response;
        var contentType = content.Headers.ContentType?.ToString() ?? "application/json";
        response.ContentType = contentType;
        var bytes = System.Text.Encoding.UTF8.GetBytes(body);
        response.ContentLength = bytes.Length;
        await response.Body.WriteAsync(bytes, transformContext.CancellationToken);
    }

    private void AppendClearCookies(HttpResponse response)
    {
        response.Cookies.Append(
            _cookieSettings.AccessTokenName,
            string.Empty,
            CreateCookieOptions(_cookieSettings.Path, expires: null, maxAge: TimeSpan.Zero));
        response.Cookies.Append(
            _cookieSettings.RefreshTokenName,
            string.Empty,
            CreateCookieOptions(RefreshCookiePath, expires: null, maxAge: TimeSpan.Zero));
        AppendLegacyRefreshCookieClear(response);
    }

    /// <summary>
    /// Старые сессии хранили refresh-cookie на Path=/. Login и logout снимают её,
    /// иначе браузер продолжит слать её вместе с cookie на префиксе auth.
    /// </summary>
    private void AppendLegacyRefreshCookieClear(HttpResponse response)
    {
        if (string.Equals(RefreshCookiePath, LegacyRefreshCookiePath, StringComparison.Ordinal))
            return;

        response.Cookies.Append(
            _cookieSettings.RefreshTokenName,
            string.Empty,
            CreateCookieOptions(LegacyRefreshCookiePath, expires: null, maxAge: TimeSpan.Zero));
    }

    private CookieOptions CreateCookieOptions(string path, DateTimeOffset? expires, TimeSpan? maxAge)
    {
        var sameSite = ParseSameSite(_cookieSettings.SameSite);
        var secure = _cookieSettings.Secure;
        // Browsers require Secure=true when SameSite=None; enforce regardless of config.
        if (sameSite == SameSiteMode.None)
            secure = true;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = path,
            Domain = _cookieSettings.Domain,
            Expires = expires,
            MaxAge = maxAge,
            IsEssential = true
        };
    }

    private string RefreshCookiePath
    {
        get
        {
            var basePath = (_authRouteSettings.BasePath ?? string.Empty).Trim();
            if (basePath.Length > 1)
                basePath = basePath.TrimEnd('/');

            return basePath.Length == 0 ? LegacyRefreshCookiePath : basePath;
        }
    }

    private bool IsLoginPath(string path) =>
        _authRouteSettings.TokenResponsePaths.Any(p =>
            path.Equals(p, StringComparison.OrdinalIgnoreCase)
            && p.EndsWith("/login", StringComparison.OrdinalIgnoreCase));

    private static DateTimeOffset ToCookieExpires(DateTime refreshExpiresAt)
    {
        var utc = refreshExpiresAt.Kind switch
        {
            DateTimeKind.Utc => refreshExpiresAt,
            DateTimeKind.Local => refreshExpiresAt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(refreshExpiresAt, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utc);
    }

    private static SameSiteMode ParseSameSite(string value)
    {
        return string.Equals(value, "Lax", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.Lax
            : string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.Strict
            : string.Equals(value, "None", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.None
            : SameSiteMode.Lax;
    }

    private sealed record AuthTokens(
        string? AccessToken,
        string? RefreshToken,
        DateTime? ExpiresAt,
        DateTime? RefreshExpiresAt);
}
