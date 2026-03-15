using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Transforms;

/// <summary>
/// YARP request transform provider for auth routes. Injects tokens from cookies into
/// requests to Identity: Authorization header from access_token, refreshToken in body for refresh/logout.
/// </summary>
internal sealed class AuthRequestTransformProvider : ITransformProvider
{
    private const int MaxBodySize = 64 * 1024; // 64 KB
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CookieSettings _cookieSettings;
    private readonly AuthRouteSettings _authRouteSettings;

    public AuthRequestTransformProvider(IOptions<CookieSettings> cookieSettings, IOptions<AuthRouteSettings> authRouteSettings)
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

        context.AddRequestTransform(ApplyAuthRequestTransform);
    }

    private async ValueTask ApplyAuthRequestTransform(RequestTransformContext transformContext)
    {
        var httpContext = transformContext.HttpContext;
        var path = httpContext.Request.Path.Value ?? "";

        // Add Authorization header from access_token cookie for all Identity requests
        var accessToken = httpContext.Request.Cookies[_cookieSettings.AccessTokenName];
        if (!string.IsNullOrEmpty(accessToken))
        {
            transformContext.ProxyRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        // For refresh and logout: inject refreshToken from cookie into body
        var isRefresh = path.Equals(_authRouteSettings.RefreshPath, StringComparison.OrdinalIgnoreCase);
        var isLogout = path.Equals(_authRouteSettings.LogoutPath, StringComparison.OrdinalIgnoreCase);
        if (!isRefresh && !isLogout)
            return;

        var refreshToken = httpContext.Request.Cookies[_cookieSettings.RefreshTokenName];
        if (string.IsNullOrEmpty(refreshToken))
            return;

        // Read existing body (middleware ensures body exists for empty requests)
        string? existingBody = null;
        if (httpContext.Request.ContentLength is > 0 and <= MaxBodySize)
        {
            httpContext.Request.EnableBuffering();
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            existingBody = await reader.ReadToEndAsync(transformContext.CancellationToken);
            httpContext.Request.Body.Position = 0;
        }

        // Merge: parse existing body, inject/overwrite refreshToken from cookie
        JsonElement? existingRoot = null;
        if (!string.IsNullOrWhiteSpace(existingBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(existingBody);
                existingRoot = doc.RootElement.Clone();
            }
            catch
            {
                // Invalid JSON: treat as empty, use only cookie
            }
        }

        var merged = MergeRefreshToken(existingRoot, refreshToken);
        var bodyBytes = Encoding.UTF8.GetBytes(merged);
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.ContentLength = bodyBytes.Length;
        httpContext.Request.ContentType = "application/json";

        // YARP reads from HttpContext.Request.Body; Content-Length must match
        transformContext.ProxyRequest.Content?.Headers.ContentLength = bodyBytes.Length;
    }

    private static string MergeRefreshToken(JsonElement? existingRoot, string refreshToken)
    {
        JsonObject obj;
        if (existingRoot.HasValue && existingRoot.Value.ValueKind == JsonValueKind.Object)
        {
            obj = JsonNode.Parse(existingRoot.Value.GetRawText())?.AsObject() ?? new JsonObject();
        }
        else
        {
            obj = new JsonObject();
        }
        obj["refreshToken"] = refreshToken;
        return obj.ToJsonString(JsonOptions);
    }
}
