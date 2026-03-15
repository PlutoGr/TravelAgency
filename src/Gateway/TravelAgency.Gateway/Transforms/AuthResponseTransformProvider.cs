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
/// On 204 from Identity for logout, sets clear-cookie headers (Max-Age=0) for access_token and refresh_token.
/// </summary>
internal sealed class AuthResponseTransformProvider : ITransformProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly CookieSettings _cookieSettings;
    private readonly AuthRouteSettings _authRouteSettings;

    public AuthResponseTransformProvider(IOptions<CookieSettings> cookieSettings, IOptions<AuthRouteSettings> authRouteSettings)
    {
        _cookieSettings = cookieSettings.Value;
        _authRouteSettings = authRouteSettings.Value;
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
        catch
        {
            await PassThroughBodyAsync(transformContext, body, content);
            return;
        }

        if (tokens?.AccessToken is null || tokens.RefreshToken is null)
        {
            await PassThroughBodyAsync(transformContext, body, content);
            return;
        }

        var response = transformContext.HttpContext.Response;
        var sameSite = ParseSameSite(_cookieSettings.SameSite);
        var secure = _cookieSettings.Secure;
        // Browsers require Secure=true when SameSite=None; enforce regardless of config.
        if (sameSite == SameSiteMode.None)
            secure = true;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = _cookieSettings.Path,
            Domain = _cookieSettings.Domain,
            IsEssential = true
        };

        response.Cookies.Append(_cookieSettings.AccessTokenName, tokens.AccessToken, cookieOptions);
        response.Cookies.Append(_cookieSettings.RefreshTokenName, tokens.RefreshToken, cookieOptions);

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
        var sameSite = ParseSameSite(_cookieSettings.SameSite);
        var secure = _cookieSettings.Secure;
        if (sameSite == SameSiteMode.None)
            secure = true;

        var clearOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = _cookieSettings.Path,
            Domain = _cookieSettings.Domain,
            MaxAge = TimeSpan.Zero,
            IsEssential = true
        };

        response.Cookies.Append(_cookieSettings.AccessTokenName, "", clearOptions);
        response.Cookies.Append(_cookieSettings.RefreshTokenName, "", clearOptions);
    }

    private static SameSiteMode ParseSameSite(string value)
    {
        return string.Equals(value, "Lax", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.Lax
            : string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.Strict
            : string.Equals(value, "None", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.None
            : SameSiteMode.Lax;
    }

    private sealed record AuthTokens(string? AccessToken, string? RefreshToken, DateTime? ExpiresAt);
}
