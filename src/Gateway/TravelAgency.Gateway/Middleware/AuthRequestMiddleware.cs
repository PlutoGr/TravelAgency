using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.Http;

namespace TravelAgency.Gateway.Middleware;

/// <summary>
/// Ensures refresh and logout requests have a body when the client sends none.
/// YARP transforms can only modify an existing body; this middleware creates one from the refresh_token cookie.
/// </summary>
internal sealed class AuthRequestMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly CookieSettings _cookieSettings;
    private readonly AuthRouteSettings _authRouteSettings;

    public AuthRequestMiddleware(
        RequestDelegate next,
        IOptions<CookieSettings> cookieSettings,
        IOptions<AuthRouteSettings> authRouteSettings)
    {
        _next = next;
        _cookieSettings = cookieSettings.Value;
        _authRouteSettings = authRouteSettings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (!IsRefreshOrLogout(path))
        {
            await _next(context);
            return;
        }

        var hasBody = context.Request.ContentLength is > 0;
        if (hasBody)
        {
            await _next(context);
            return;
        }

        var refreshToken = CookieHeaderReader.ReadFirst(
            context.Request.Headers.Cookie.ToString(),
            _cookieSettings.RefreshTokenName);
        if (string.IsNullOrEmpty(refreshToken))
        {
            await _next(context);
            return;
        }

        var body = JsonSerializer.Serialize(new { refreshToken }, JsonOptions);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        context.Request.ContentType = "application/json";

        context.Features.Set<IHttpRequestBodyDetectionFeature>(new BodyDetectionFeature());

        await _next(context);
    }

    private bool IsRefreshOrLogout(string path)
    {
        var refresh = Endpoint("refresh");
        var logout = Endpoint("logout");
        return path.Equals(refresh, StringComparison.OrdinalIgnoreCase)
            || path.Equals(logout, StringComparison.OrdinalIgnoreCase);
    }

    private string Endpoint(string action)
    {
        var prefix = (_authRouteSettings.BasePath ?? string.Empty).Trim();
        if (prefix.Length > 1)
            prefix = prefix.TrimEnd('/');
        return prefix.Length == 0 ? "/" + action : prefix + "/" + action;
    }

    private sealed class BodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
