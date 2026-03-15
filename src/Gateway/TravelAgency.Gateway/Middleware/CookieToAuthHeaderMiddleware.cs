using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Middleware;

/// <summary>
/// Injects Authorization: Bearer from access_token cookie when the request has no Authorization header.
/// Runs before UseAuthentication so JWT middleware can authenticate cookie-based requests.
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
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Request.Headers.Authorization = $"Bearer {accessToken}";
            }
        }

        await _next(context);
    }
}
