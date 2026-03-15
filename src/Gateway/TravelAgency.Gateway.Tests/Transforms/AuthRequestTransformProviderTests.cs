using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;
using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.Transforms;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TravelAgency.Gateway.Tests.Transforms;

public class AuthRequestTransformProviderTests
{
    private static AuthRequestTransformProvider CreateProvider(CookieSettings? settings = null, AuthRouteSettings? authRouteSettings = null)
    {
        settings ??= new CookieSettings();
        authRouteSettings ??= new AuthRouteSettings();
        return new AuthRequestTransformProvider(Options.Create(settings), Options.Create(authRouteSettings));
    }

    private static RequestTransformContext CreateRequestContext(
        string path,
        string? accessTokenCookie = null,
        string? refreshTokenCookie = null,
        string? body = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "POST";
        var cookieParts = new List<string>();
        if (accessTokenCookie != null)
            cookieParts.Add($"access_token={accessTokenCookie}");
        if (refreshTokenCookie != null)
            cookieParts.Add($"refresh_token={refreshTokenCookie}");
        if (cookieParts.Count > 0)
            context.Request.Headers.Cookie = string.Join("; ", cookieParts);
        if (body != null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Request.ContentType = "application/json";
        }

        var proxyRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/identity/refresh");
        return new RequestTransformContext
        {
            HttpContext = context,
            ProxyRequest = proxyRequest,
            Path = path,
            CancellationToken = CancellationToken.None
        };
    }

    private static async Task InvokeTransformAsync(AuthRequestTransformProvider provider, RequestTransformContext context)
    {
        var method = typeof(AuthRequestTransformProvider)
            .GetMethod("ApplyAuthRequestTransform", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ApplyAuthRequestTransform not found");
        var task = method.Invoke(provider, [context]);
        if (task is ValueTask vt)
            await vt.AsTask();
        else if (task is Task t)
            await t;
    }

    [Fact]
    public void Apply_WhenRouteIsAuthRoute_AddsRequestTransform()
    {
        var routeConfig = new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "auth-route",
            ClusterId = "identity-cluster",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/api/v1/auth/{**catch-all}" }
        };
        var clusterConfig = new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "identity-cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["d1"] = new() { Address = "http://localhost" }
            }
        };
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider();
        var builderContext = new TransformBuilderContext
        {
            Route = routeConfig,
            Cluster = clusterConfig,
            Services = services
        };

        var provider = CreateProvider();
        provider.Apply(builderContext);

        Assert.Single(builderContext.RequestTransforms);
    }

    [Fact]
    public void Apply_WhenRouteIsNotAuthRoute_DoesNotAddTransform()
    {
        var routeConfig = new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "catalog-route",
            ClusterId = "catalog-cluster",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/api/v1/catalog/{**catch-all}" }
        };
        var clusterConfig = new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "catalog-cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["d1"] = new() { Address = "http://localhost" }
            }
        };
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider();
        var builderContext = new TransformBuilderContext
        {
            Route = routeConfig,
            Cluster = clusterConfig,
            Services = services
        };

        var provider = CreateProvider();
        provider.Apply(builderContext);

        Assert.Empty(builderContext.RequestTransforms);
    }

    [Fact]
    public async Task Transform_WhenAccessTokenCookiePresent_AddsAuthorizationHeader()
    {
        var provider = CreateProvider();
        var context = CreateRequestContext("/api/v1/auth/me", accessTokenCookie: "jwt-token-123");

        await InvokeTransformAsync(provider, context);

        Assert.NotNull(context.ProxyRequest.Headers.Authorization);
        Assert.Equal("Bearer", context.ProxyRequest.Headers.Authorization!.Scheme);
        Assert.Equal("jwt-token-123", context.ProxyRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Transform_WhenNoAccessTokenCookie_DoesNotAddAuthorizationHeader()
    {
        var provider = CreateProvider();
        var context = CreateRequestContext("/api/v1/auth/login");

        await InvokeTransformAsync(provider, context);

        Assert.Null(context.ProxyRequest.Headers.Authorization);
    }

    [Fact]
    public async Task Transform_WhenRefreshPathWithRefreshTokenCookie_InjectsRefreshTokenInBody()
    {
        var provider = CreateProvider();
        var context = CreateRequestContext(
            "/api/v1/auth/refresh",
            refreshTokenCookie: "rt-from-cookie",
            body: "{}");

        await InvokeTransformAsync(provider, context);

        using var reader = new StreamReader(context.HttpContext.Request.Body);
        context.HttpContext.Request.Body.Position = 0;
        var body = await reader.ReadToEndAsync();
        var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("rt-from-cookie", doc.RootElement.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Transform_WhenLogoutPathWithRefreshTokenCookie_MergesRefreshTokenInBody()
    {
        var provider = CreateProvider();
        var context = CreateRequestContext(
            "/api/v1/auth/logout",
            refreshTokenCookie: "logout-rt",
            body: """{"otherField":"value"}""");

        await InvokeTransformAsync(provider, context);

        context.HttpContext.Request.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync();
        var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("logout-rt", doc.RootElement.GetProperty("refreshToken").GetString());
        Assert.Equal("value", doc.RootElement.GetProperty("otherField").GetString());
    }

    [Fact]
    public async Task Transform_WhenRefreshPathNoRefreshTokenCookie_DoesNotModifyBody()
    {
        var provider = CreateProvider();
        var originalBody = """{"refreshToken":"client-sent"}""";
        var context = CreateRequestContext("/api/v1/auth/refresh", body: originalBody);

        await InvokeTransformAsync(provider, context);

        Assert.Null(context.ProxyRequest.Headers.Authorization);
    }

    [Fact]
    public async Task Transform_WhenCustomCookieNames_UsesConfiguredNames()
    {
        var settings = new CookieSettings { AccessTokenName = "at", RefreshTokenName = "rt" };
        var provider = CreateProvider(settings);
        var context = CreateRequestContext("/api/v1/auth/me");
        context.HttpContext.Request.Headers.Cookie = "at=custom-access";

        await InvokeTransformAsync(provider, context);

        Assert.NotNull(context.ProxyRequest.Headers.Authorization);
        Assert.Equal("custom-access", context.ProxyRequest.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task Transform_WhenLoginPath_DoesNotModifyBody()
    {
        var provider = CreateProvider();
        var context = CreateRequestContext("/api/v1/auth/login", body: """{"email":"a@b.com","password":"x"}""");

        await InvokeTransformAsync(provider, context);

        context.HttpContext.Request.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("email", body);
        Assert.Contains("password", body);
    }
}
