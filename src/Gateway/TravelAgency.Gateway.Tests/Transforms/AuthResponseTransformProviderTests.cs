using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;
using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.Transforms;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TravelAgency.Gateway.Tests.Transforms;

public class AuthResponseTransformProviderTests
{
    private static AuthResponseTransformProvider CreateProvider(CookieSettings? settings = null, AuthRouteSettings? authRouteSettings = null)
    {
        settings ??= new CookieSettings();
        authRouteSettings ??= new AuthRouteSettings();
        return new AuthResponseTransformProvider(Options.Create(settings), Options.Create(authRouteSettings));
    }

    private static ResponseTransformContext CreateTransformContext(
        string path,
        HttpStatusCode statusCode,
        string? responseBody,
        CookieSettings? cookieSettings = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        HttpContent? content = null;
        if (responseBody != null)
        {
            content = new StringContent(responseBody, Encoding.UTF8, "application/json");
        }

        var proxyResponse = new HttpResponseMessage(statusCode) { Content = content };

        return new ResponseTransformContext
        {
            HttpContext = context,
            ProxyResponse = proxyResponse,
            CancellationToken = CancellationToken.None
        };
    }

    private static async Task InvokeTransformAsync(AuthResponseTransformProvider provider, ResponseTransformContext context)
    {
        var method = typeof(AuthResponseTransformProvider)
            .GetMethod("ApplyAuthResponseTransform", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ApplyAuthResponseTransform not found");
        var task = method.Invoke(provider, [context]);
        if (task is ValueTask vt)
            await vt.AsTask();
        else if (task is Task t)
            await t;
    }

    [Fact]
    public void Apply_WhenRouteIsAuthRoute_AddsResponseTransform()
    {
        // Arrange
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

        // Act
        provider.Apply(builderContext);

        // Assert
        Assert.Single(builderContext.ResponseTransforms);
    }

    [Fact]
    public void Apply_WhenRouteIsNotAuthRoute_DoesNotAddTransform()
    {
        // Arrange
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

        // Act
        provider.Apply(builderContext);

        // Assert
        Assert.Empty(builderContext.ResponseTransforms);
    }

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/refresh")]
    public async Task Transform_When200WithValidTokens_SetsCookiesAndReplacesBody(string path)
    {
        // Arrange
        var tokenJson = JsonSerializer.Serialize(new
        {
            accessToken = "access-123",
            refreshToken = "refresh-456",
            expiresAt = DateTime.UtcNow.AddHours(1)
        });
        var provider = CreateProvider();
        var context = CreateTransformContext(path, HttpStatusCode.OK, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        Assert.True(context.SuppressResponseBody);
        var setCookieList = context.HttpContext.Response.Headers.SetCookie.ToList();
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("access_token=", StringComparison.Ordinal));
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("refresh_token=", StringComparison.Ordinal));

        context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.HttpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Transform_When201WithValidTokens_SetsCookiesAndReplacesBody()
    {
        // Arrange
        var tokenJson = JsonSerializer.Serialize(new
        {
            accessToken = "at",
            refreshToken = "rt",
            expiresAt = (DateTime?)null
        });
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/register", HttpStatusCode.Created, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        Assert.True(context.SuppressResponseBody);
        var setCookieList = context.HttpContext.Response.Headers.SetCookie.ToList();
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("access_token=", StringComparison.Ordinal));
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("refresh_token=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Transform_WhenCustomCookieNames_UsesConfiguredNames()
    {
        // Arrange
        var settings = new CookieSettings
        {
            AccessTokenName = "at",
            RefreshTokenName = "rt"
        };
        var tokenJson = JsonSerializer.Serialize(new { accessToken = "x", refreshToken = "y" });
        var provider = CreateProvider(settings);
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        var setCookieList = context.HttpContext.Response.Headers.SetCookie.ToList();
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("at=", StringComparison.Ordinal));
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("rt=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Transform_WhenDomainConfigured_IncludesDomainInSetCookie()
    {
        // Arrange
        var settings = new CookieSettings { Domain = ".travelagency.com" };
        var tokenJson = JsonSerializer.Serialize(new { accessToken = "x", refreshToken = "y" });
        var provider = CreateProvider(settings);
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        var setCookieList = context.HttpContext.Response.Headers.SetCookie.ToList();
        Assert.All(setCookieList, h => Assert.Contains("domain=.travelagency.com", h, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Transform_When204OnLogout_SetsClearCookies()
    {
        // Arrange
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/logout", HttpStatusCode.NoContent, null);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        Assert.True(context.SuppressResponseBody);
        var setCookieList = context.HttpContext.Response.Headers.SetCookie.ToList();
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("access_token=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("refresh_token=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Transform_When204OnLogout_WithCustomCookieNames_UsesConfiguredNames()
    {
        // Arrange
        var settings = new CookieSettings { AccessTokenName = "at", RefreshTokenName = "rt" };
        var provider = CreateProvider(settings);
        var context = CreateTransformContext("/api/v1/auth/logout", HttpStatusCode.NoContent, null);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        var setCookieList = context.HttpContext.Response.Headers.SetCookie.ToList();
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("at=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieList, h => h != null && h.StartsWith("rt=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Transform_WhenLogoutPathButNot204_DoesNotModifyResponse()
    {
        // Arrange
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/logout", HttpStatusCode.OK, "{}");

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert - 200 on logout path does not trigger clear (Identity would return 204 on success)
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Transform_WhenNonAuthPath_DoesNotModifyResponse()
    {
        // Arrange
        var tokenJson = JsonSerializer.Serialize(new { accessToken = "x", refreshToken = "y" });
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/me", HttpStatusCode.OK, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        Assert.False(context.SuppressResponseBody);
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Transform_When404_DoesNotModifyResponse()
    {
        // Arrange
        var tokenJson = JsonSerializer.Serialize(new { accessToken = "x", refreshToken = "y" });
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.NotFound, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        Assert.False(context.SuppressResponseBody);
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Transform_WhenEmptyBody_DoesNotModifyResponse()
    {
        // Arrange
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, "");

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert
        Assert.False(context.SuppressResponseBody);
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Transform_WhenInvalidJson_PassesThroughOriginalBody()
    {
        // Arrange
        var invalidJson = "not-json";
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, invalidJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert: no cookies, original body passed through (stream was consumed, we write it back)
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
        context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(context.HttpContext.Response.Body))
        {
            var responseBody = await reader.ReadToEndAsync();
            Assert.Equal(invalidJson, responseBody);
        }
    }

    [Fact]
    public async Task Transform_WhenMissingAccessToken_PassesThroughOriginalBody()
    {
        // Arrange
        var tokenJson = JsonSerializer.Serialize(new { refreshToken = "rt" });
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert: no cookies, original body passed through
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
        context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(context.HttpContext.Response.Body))
        {
            var responseBody = await reader.ReadToEndAsync();
            Assert.Equal(tokenJson, responseBody);
        }
    }

    [Fact]
    public async Task Transform_WhenMissingRefreshToken_PassesThroughOriginalBody()
    {
        // Arrange
        var tokenJson = JsonSerializer.Serialize(new { accessToken = "at" });
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, tokenJson);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert: no cookies, original body passed through
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
        context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(context.HttpContext.Response.Body))
        {
            var responseBody = await reader.ReadToEndAsync();
            Assert.Equal(tokenJson, responseBody);
        }
    }

    [Fact]
    public async Task Transform_WhenNoTokensInBody_PassesThroughOriginalBody()
    {
        // Arrange: mock backend returns non-token JSON (e.g. redirect path)
        var bodyWithoutTokens = JsonSerializer.Serialize(new { path = "/identity/login" });
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, bodyWithoutTokens);

        // Act
        await InvokeTransformAsync(provider, context);

        // Assert: no cookies, original body passed through (not empty)
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
        context.HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.HttpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.Equal(bodyWithoutTokens, responseBody);
    }

    [Fact]
    public void ValidateRoute_DoesNotThrow()
    {
        var provider = CreateProvider();
        var validationContext = new TransformRouteValidationContext
        {
            Route = new Yarp.ReverseProxy.Configuration.RouteConfig { RouteId = "auth-route" },
            Services = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider()
        };
        var ex = Record.Exception(() => provider.ValidateRoute(validationContext));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateCluster_DoesNotThrow()
    {
        var provider = CreateProvider();
        var validationContext = new TransformClusterValidationContext
        {
            Cluster = new Yarp.ReverseProxy.Configuration.ClusterConfig
            {
                ClusterId = "identity-cluster",
                Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
                {
                    ["d1"] = new() { Address = "http://localhost" }
                }
            },
            Services = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider()
        };
        var ex = Record.Exception(() => provider.ValidateCluster(validationContext));
        Assert.Null(ex);
    }
}
