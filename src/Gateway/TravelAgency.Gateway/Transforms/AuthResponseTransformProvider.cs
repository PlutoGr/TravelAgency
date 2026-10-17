using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Transforms;

/// <summary>
/// YARP transform for login, register, refresh and logout.
/// A usable token response becomes httpOnly cookies and {"success":true}.
/// Access cookie Expires is the JWT expiry. Refresh cookie Expires is refreshExpiresAt.
/// A token route without both tokens is 502 with an empty body and no cookies.
/// Refresh cookie path is the auth prefix. Login, register, refresh and logout also
/// delete the previous refresh cookie on Cookie:Path.
/// </summary>
internal sealed class AuthResponseTransformProvider : ITransformProvider
{
    private const string InvalidAuthCode = "auth_upstream_invalid";
    private const string InvalidAuthMessage = "Authentication is temporarily unavailable.";

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
        var isLogout = path.Equals(_authRouteSettings.LogoutPath, StringComparison.OrdinalIgnoreCase);

        if (isLogout && status is HttpStatusCode.NoContent or HttpStatusCode.Unauthorized)
        {
            var logoutResponse = transformContext.HttpContext.Response;
            logoutResponse.StatusCode = (int)status;
            AppendClearCookies(logoutResponse);
            if (status == HttpStatusCode.NoContent)
                transformContext.SuppressResponseBody = true;
            return;
        }

        if (status is not (HttpStatusCode.OK or HttpStatusCode.Created))
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
        catch (JsonException)
        {
            await RejectAuthResponseAsync(transformContext, "json");
            return;
        }

        var hasAccess = !string.IsNullOrWhiteSpace(tokens?.AccessToken);
        var hasRefresh = !string.IsNullOrWhiteSpace(tokens?.RefreshToken);
        if (!hasAccess || !hasRefresh || tokens is null)
        {
            await RejectAuthResponseAsync(transformContext, "incomplete-tokens", emptyBody: !hasAccess && !hasRefresh);
            return;
        }

        if (!IsUsableExpiry(tokens.ExpiresAt, out var accessExpires))
        {
            await RejectAuthResponseAsync(transformContext, "access-expiry");
            return;
        }

        if (!IsUsableExpiry(tokens.RefreshExpiresAt, out var refreshExpires))
        {
            await RejectAuthResponseAsync(transformContext, "refresh-expiry");
            return;
        }

        var response = transformContext.HttpContext.Response;
        response.Cookies.Append(
            _cookieSettings.AccessTokenName,
            tokens.AccessToken!,
            CreateCookieOptions(_cookieSettings.Path, accessExpires, maxAge: null));
        response.Cookies.Append(
            _cookieSettings.RefreshTokenName,
            tokens.RefreshToken!,
            CreateCookieOptions(RefreshCookiePath, refreshExpires, maxAge: null));
        AppendLegacyRefreshCookieClear(response);

        transformContext.SuppressResponseBody = true;

        var successBody = JsonSerializer.Serialize(new { success = true });
        response.ContentType = "application/json";
        response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(successBody);
        await response.WriteAsync(successBody, transformContext.CancellationToken);
    }

    /// <summary>
    /// Drops the upstream body. The log sees only the reason, never tokens.
    /// A response with no bound tokens is an empty 502. Other rejects keep the generic JSON.
    /// </summary>
    private async ValueTask RejectAuthResponseAsync(
        ResponseTransformContext transformContext,
        string reason,
        bool emptyBody = false)
    {
        _logger.LogError("Rejected auth token response: {Reason}", reason);
        transformContext.SuppressResponseBody = true;

        var response = transformContext.HttpContext.Response;
        response.StatusCode = StatusCodes.Status502BadGateway;
        if (emptyBody)
        {
            response.ContentLength = 0;
            return;
        }

        response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { code = InvalidAuthCode, message = InvalidAuthMessage });
        var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
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
    /// Previous sessions stored refresh-cookie on Cookie:Path. A new cookie on the auth
    /// prefix does not replace it: same name and different Path are different cookies.
    /// </summary>
    private void AppendLegacyRefreshCookieClear(HttpResponse response)
    {
        var legacyPath = LegacyRefreshCookiePath;
        if (string.Equals(RefreshCookiePath, legacyPath, StringComparison.Ordinal))
            return;

        response.Cookies.Append(
            _cookieSettings.RefreshTokenName,
            string.Empty,
            CreateCookieOptions(legacyPath, expires: null, maxAge: TimeSpan.Zero));
    }

    private CookieOptions CreateCookieOptions(string path, DateTimeOffset? expires, TimeSpan? maxAge)
    {
        var sameSite = ParseSameSite(_cookieSettings.SameSite);
        var secure = _cookieSettings.Secure;
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

    private string LegacyRefreshCookiePath
    {
        get
        {
            var path = (_cookieSettings.Path ?? string.Empty).Trim();
            if (path.Length > 1)
                path = path.TrimEnd('/');
            return path.Length == 0 ? "/" : path;
        }
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

    private static bool IsUsableExpiry(DateTime? value, out DateTimeOffset expires)
    {
        expires = default;
        if (value is null)
            return false;

        var utc = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };

        if (utc.Year < 2)
            return false;

        expires = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
        return expires > DateTimeOffset.UtcNow;
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
