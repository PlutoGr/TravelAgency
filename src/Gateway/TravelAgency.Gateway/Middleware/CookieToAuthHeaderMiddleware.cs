using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Middleware;

/// <summary>
/// Injects Authorization: Bearer when the request has no Authorization header.
/// The token is taken from the access_token cookie, or, if the cookie is absent,
/// from the access_token query string (SignalR WebSocket: the browser cannot set
/// an Authorization header on the upgrade).
/// Runs before UseAuthentication so JWT middleware can authenticate the request.
/// On dev, nginx basic auth occupies Authorization and that header is removed
/// before the gateway, so the JWT arrives as the cookie or the SignalR query value.
/// </summary>
internal sealed class CookieToAuthHeaderMiddleware
{
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
            if (string.IsNullOrEmpty(accessToken))
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
}
