using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Middleware;

/// <summary>
/// Injects Authorization: Bearer when the request has no Authorization header.
/// The token is taken from the access_token cookie on every route.
/// The access_token query string is read only for the chat hub path
/// (/api/v1/chat/hubs/...), because a browser WebSocket cannot set Authorization.
/// Query tokens on other routes are ignored: they leak into access logs, history and Referer.
/// Runs before UseAuthentication so JWT middleware can authenticate the request.
/// On dev, nginx basic auth occupies Authorization and that header is removed
/// before the gateway, so the JWT arrives as the cookie or, on the hub, as the SignalR query value.
/// </summary>
internal sealed class CookieToAuthHeaderMiddleware
{
    /// <summary>
    /// Segment prefix of the SignalR hub. StartsWithSegments rejects lookalikes such as /api/v1/chat/hubsX.
    /// </summary>
    private static readonly PathString ChatHubPathPrefix = new("/api/v1/chat/hubs");

    private readonly RequestDelegate _next;
    private readonly CookieSettings _cookieSettings;

    public CookieToAuthHeaderMiddleware(RequestDelegate next, IOptions<CookieSettings> cookieSettings)
    {
        _next = next;
        _cookieSettings = cookieSettings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            var accessToken = context.Request.Cookies[_cookieSettings.AccessTokenName];
            if (string.IsNullOrEmpty(accessToken) && IsChatHubPath(context.Request.Path))
            {
                accessToken = context.Request.Query[_cookieSettings.AccessTokenName].ToString();
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Request.Headers.Authorization = $"Bearer {accessToken}";
            }
        }

        await _next(context);
    }

    private static bool IsChatHubPath(PathString path) =>
        path.StartsWithSegments(ChatHubPathPrefix, StringComparison.OrdinalIgnoreCase);
}
