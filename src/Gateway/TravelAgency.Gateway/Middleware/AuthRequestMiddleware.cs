using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Middleware;

/// <summary>
/// Ensures refresh and logout requests have a body when the client sends none.
/// YARP transforms can only modify an existing body; this middleware creates one from the refresh_token cookie.
/// </summary>
internal sealed class AuthRequestMiddleware
{
    private static readonly string[] RefreshLogoutPaths = ["/api/v1/auth/refresh", "/api/v1/auth/logout"];
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly CookieSettings _cookieSettings;

    public AuthRequestMiddleware(RequestDelegate next, IOptions<CookieSettings> cookieSettings)
    {
        _next = next;
        _cookieSettings = cookieSettings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (!RefreshLogoutPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
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

        var refreshToken = context.Request.Cookies[_cookieSettings.RefreshTokenName];
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

    private sealed class BodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
