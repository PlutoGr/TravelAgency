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
/// Token paths are matched without a trailing slash and without regard to case.
/// A token route answer other than a usable 200 or 201 is 502, except Identity 4xx, which is passed through with no cookies.
/// A 2xx on any other auth path is 502 when the body contains a token field.
/// refreshExpiresAt later than RefreshTokenExpirationDays plus one day is 502.
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
        var path = NormalizePath(transformContext.HttpContext.Request.Path.Value);
        var isLogout = path.Equals(NormalizePath(_authRouteSettings.LogoutPath), StringComparison.OrdinalIgnoreCase);
        var isRefresh = path.Equals(NormalizePath(_authRouteSettings.RefreshPath), StringComparison.OrdinalIgnoreCase);
        var isTokenPath = IsTokenResponsePath(path);

        if (isLogout && status is HttpStatusCode.NoContent or HttpStatusCode.Unauthorized)
        {
            var logoutResponse = transformContext.HttpContext.Response;
            logoutResponse.StatusCode = (int)status;
            AppendClearCookies(logoutResponse);
            if (status == HttpStatusCode.NoContent)
                transformContext.SuppressResponseBody = true;
            return;
        }

        if (isRefresh && status == HttpStatusCode.Unauthorized)
        {
            var refreshResponse = transformContext.HttpContext.Response;
            refreshResponse.StatusCode = StatusCodes.Status401Unauthorized;
            AppendClearCookies(refreshResponse);
            return;
        }

        if (isTokenPath && status is not (HttpStatusCode.OK or HttpStatusCode.Created))
        {
            if (Is4xx(status))
                return;
            if (Is2xx(status))
                await RejectAuthResponseAsync(transformContext, "unexpected-status");
            return;
        }

        if (!Is2xx(status))
            return;

        var content = transformContext.ProxyResponse?.Content;
        if (content is null)
        {
            if (isTokenPath)
                await RejectAuthResponseAsync(transformContext, "empty");
            return;
        }

        string? body;
        await using (var stream = await content.ReadAsStreamAsync(transformContext.CancellationToken))
        {
            using var reader = new StreamReader(stream);
            body = await reader.ReadToEndAsync(transformContext.CancellationToken);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            if (isTokenPath)
                await RejectAuthResponseAsync(transformContext, "empty");
            return;
        }

        if (!isTokenPath)
        {
            if (JsonContainsTokenField(body))
                await RejectAuthResponseAsync(transformContext, "unexpected-tokens");
            else
                await CopyUpstreamBodyAsync(transformContext, body);
            return;
        }

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

        if (!IsUsableRefreshExpiry(tokens.RefreshExpiresAt, out var refreshExpires))
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
    /// The transform has to read the upstream stream to look for token fields.
    /// A safe body is written back, otherwise YARP would copy an already consumed stream.
    /// </summary>
    private static async ValueTask CopyUpstreamBodyAsync(ResponseTransformContext transformContext, string body)
    {
        transformContext.SuppressResponseBody = true;
        var response = transformContext.HttpContext.Response;
        if (transformContext.ProxyResponse is not null)
            response.StatusCode = (int)transformContext.ProxyResponse.StatusCode;

        var contentType = transformContext.ProxyResponse?.Content?.Headers.ContentType?.ToString();
        if (!string.IsNullOrEmpty(contentType))
            response.ContentType = contentType;

        var bytes = System.Text.Encoding.UTF8.GetBytes(body);
        response.ContentLength = bytes.Length;
        await response.Body.WriteAsync(bytes, transformContext.CancellationToken);
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

    private bool IsTokenResponsePath(string normalizedPath) =>
        _authRouteSettings.TokenResponsePaths.Any(p =>
            string.Equals(NormalizePath(p), normalizedPath, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Trailing slash and letter case are not different routes.
    /// /api/v1/auth/login/ and /api/v1/auth/Login are the login path.
    /// </summary>
    private static string NormalizePath(string? path)
    {
        var value = (path ?? string.Empty).Trim();
        if (value.Length > 1)
            value = value.TrimEnd('/');
        return value;
    }

    private static bool Is2xx(HttpStatusCode? status) =>
        status is >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices;

    private static bool Is4xx(HttpStatusCode? status) =>
        status is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError;

    /// <summary>
    /// accessToken and refreshToken in any case, including snake_case.
    /// </summary>
    private static bool JsonContainsTokenField(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var name = new string(property.Name.Where(ch => ch != '_').ToArray());
                if (name.Equals("accessToken", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("refreshToken", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool IsUsableRefreshExpiry(DateTime? value, out DateTimeOffset expires)
    {
        if (!IsUsableExpiry(value, out expires))
            return false;

        var limit = DateTimeOffset.UtcNow.AddDays(_authRouteSettings.RefreshTokenExpirationDays + 1);
        return expires <= limit;
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
