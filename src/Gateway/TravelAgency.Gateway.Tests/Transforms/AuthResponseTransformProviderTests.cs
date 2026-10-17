using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.Transforms;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TravelAgency.Gateway.Tests.Transforms;

public class AuthResponseTransformProviderTests
{
    private static AuthResponseTransformProvider CreateProvider(
        CookieSettings? settings = null,
        AuthRouteSettings? authRouteSettings = null,
        ILogger<AuthResponseTransformProvider>? logger = null)
    {
        settings ??= new CookieSettings();
        authRouteSettings ??= new AuthRouteSettings();
        return new AuthResponseTransformProvider(
            Options.Create(settings),
            Options.Create(authRouteSettings),
            logger ?? NullLogger<AuthResponseTransformProvider>.Instance);
    }

    private static ResponseTransformContext CreateTransformContext(
        string path,
        HttpStatusCode statusCode,
        string? responseBody,
        CookieSettings? cookieSettings = null)
    {
        var context = new DefaultHttpContext();
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            context.Request.QueryString = new QueryString(path[queryIndex..]);
            path = path[..queryIndex];
        }

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

    private static readonly DateTime SampleAccessExpiresAt = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SampleRefreshExpiresAt = new(2026, 12, 1, 15, 30, 0, DateTimeKind.Utc);

    private static string TokenJson(
        string accessToken = "access-123",
        string refreshToken = "refresh-456",
        bool includeRefreshExpiresAt = true,
        bool includeExpiresAt = true,
        DateTime? expiresAt = null,
        DateTime? refreshExpiresAt = null)
    {
        if (!includeExpiresAt && !includeRefreshExpiresAt)
        {
            return JsonSerializer.Serialize(new { accessToken, refreshToken });
        }

        if (!includeRefreshExpiresAt)
        {
            return JsonSerializer.Serialize(new
            {
                accessToken,
                refreshToken,
                expiresAt = expiresAt ?? SampleAccessExpiresAt
            });
        }

        if (!includeExpiresAt)
        {
            return JsonSerializer.Serialize(new
            {
                accessToken,
                refreshToken,
                refreshExpiresAt = refreshExpiresAt ?? SampleRefreshExpiresAt
            });
        }

        return JsonSerializer.Serialize(new
        {
            accessToken,
            refreshToken,
            expiresAt = expiresAt ?? SampleAccessExpiresAt,
            refreshExpiresAt = refreshExpiresAt ?? SampleRefreshExpiresAt
        });
    }

    private static string ExpiresAttribute(DateTime value) =>
        "expires=" + value.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);

    private static bool HasCookiePath(string header, string path)
    {
        var marker = $"path={path}";
        var index = header.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return false;

        var end = index + marker.Length;
        return end >= header.Length || header[end] is ';' or ' ';
    }

    private static async Task<string> ReadBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static void AssertGeneric502(HttpResponse response, string body, params string[] secrets)
    {
        Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
        Assert.Equal(0, response.Headers.SetCookie.Count);
        Assert.Contains("auth_upstream_invalid", body, StringComparison.Ordinal);
        Assert.Contains("Authentication is temporarily unavailable.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);
        foreach (var secret in secrets)
            Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
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
        var tokenJson = TokenJson();
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
        var tokenJson = TokenJson(accessToken: "at", refreshToken: "rt");
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
        var tokenJson = TokenJson(accessToken: "x", refreshToken: "y");
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
        var tokenJson = TokenJson(accessToken: "x", refreshToken: "y");
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
    public async Task Transform_WhenNonTokenPathHasNoTokenField_PassesBodyThrough()
    {
        var provider = CreateProvider();
        var upstream = """{"path":"/identity/me"}""";
        var context = CreateTransformContext("/api/v1/auth/me", HttpStatusCode.OK, upstream);

        await InvokeTransformAsync(provider, context);

        Assert.Equal(StatusCodes.Status200OK, context.HttpContext.Response.StatusCode);
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
        Assert.Equal(upstream, await ReadBodyAsync(context.HttpContext.Response));
    }

    [Theory]
    [InlineData("{\"accessToken\":\"at-secret\"}")]
    [InlineData("{\"REFRESH_TOKEN\":\"rt-secret\"}")]
    [InlineData("{\"access_token\":\"at-secret\",\"refresh_token\":\"rt-secret\"}")]
    public async Task Transform_WhenNonTokenPathContainsTokenField_Returns502(string tokenJson)
    {
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/me", HttpStatusCode.OK, tokenJson);

        await InvokeTransformAsync(provider, context);

        var responseBody = await ReadBodyAsync(context.HttpContext.Response);
        AssertGeneric502(context.HttpContext.Response, responseBody, "at-secret", "rt-secret", tokenJson);
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
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, "");

        await InvokeTransformAsync(provider, context);

        Assert.False(context.SuppressResponseBody);
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
    }

    public static TheoryData<string> NormalizedTokenPaths { get; } = new()
    {
        "/api/v1/auth/login/",
        "/api/v1/auth/Login",
        "/api/v1/auth/Login/",
        "/api/v1/auth/%6Cogin",
        "/api/v1/auth/./login",
        "/api/v1/auth/login?x=1",
        "/api/v1/auth/register/",
        "/api/v1/auth/Register",
        "/api/v1/auth/Register/",
        "/api/v1/auth/%72egister",
        "/api/v1/auth/./register",
        "/api/v1/auth/register?x=1",
        "/api/v1/auth/refresh/",
        "/api/v1/auth/Refresh",
        "/api/v1/auth/Refresh/",
        "/api/v1/auth/%72efresh",
        "/api/v1/auth/./refresh",
        "/api/v1/auth/refresh?x=1",
    };

    [Theory]
    [MemberData(nameof(NormalizedTokenPaths))]
    public async Task Transform_WhenTokenPathIsNormalized_SetsCookiesNotRawTokens(string path)
    {
        var tokenJson = TokenJson();
        var provider = CreateProvider();
        var context = CreateTransformContext(path, HttpStatusCode.OK, tokenJson);

        await InvokeTransformAsync(provider, context);

        Assert.True(context.SuppressResponseBody);
        var body = await ReadBodyAsync(context.HttpContext.Response);
        Assert.Contains("\"success\":true", body, StringComparison.Ordinal);
        Assert.DoesNotContain("access-123", body, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh-456", body, StringComparison.Ordinal);
        Assert.Contains(
            context.HttpContext.Response.Headers.SetCookie,
            h => h != null && h.StartsWith("access_token=access-123", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "/api/v1/auth/session")]
    [InlineData(HttpStatusCode.Accepted, "/api/v1/auth/other")]
    [InlineData(HttpStatusCode.OK, "/api/v1/auth/./me")]
    public async Task Transform_WhenAnyAuthPath2xxContainsTokenField_Returns502(HttpStatusCode status, string path)
    {
        var tokenJson = "{\"accessToken\":\"at-secret\",\"refresh_token\":\"rt-secret\"}";
        var provider = CreateProvider();
        var context = CreateTransformContext(path, status, tokenJson);

        await InvokeTransformAsync(provider, context);

        var responseBody = await ReadBodyAsync(context.HttpContext.Response);
        AssertGeneric502(context.HttpContext.Response, responseBody, "at-secret", "rt-secret");
    }

    [Fact]
    public async Task Transform_WhenInvalidJson_Returns502WithoutUpstreamBody()
    {
        var logger = new ListLogger();
        var invalidJson = "not-json-accessToken-secret";
        var provider = CreateProvider(logger: logger);
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, invalidJson);

        await InvokeTransformAsync(provider, context);

        var responseBody = await ReadBodyAsync(context.HttpContext.Response);
        AssertGeneric502(context.HttpContext.Response, responseBody, invalidJson);
        Assert.DoesNotContain(invalidJson, string.Join('\n', logger.Messages), StringComparison.Ordinal);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Theory]
    [InlineData("refresh-only")]
    [InlineData("access-only")]
    public async Task Transform_WhenOnlyOneToken_Returns502WithoutTokens(string shape)
    {
        var logger = new ListLogger();
        var tokenJson = shape == "refresh-only"
            ? JsonSerializer.Serialize(new { refreshToken = "rt-secret" })
            : JsonSerializer.Serialize(new { accessToken = "at-secret" });
        var provider = CreateProvider(logger: logger);
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, tokenJson);

        await InvokeTransformAsync(provider, context);

        var responseBody = await ReadBodyAsync(context.HttpContext.Response);
        AssertGeneric502(context.HttpContext.Response, responseBody, "at-secret", "rt-secret", tokenJson);
        var log = string.Join('\n', logger.Messages);
        Assert.DoesNotContain("at-secret", log, StringComparison.Ordinal);
        Assert.DoesNotContain("rt-secret", log, StringComparison.Ordinal);
        Assert.DoesNotContain(tokenJson, log, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{\"access_token\":\"at-secret\",\"refresh_token\":\"rt-secret\"}")]
    [InlineData("{\"path\":\"/identity/login\"}")]
    public async Task Transform_WhenTokenRouteLacksBothTokens_Returns502WithEmptyBody(string upstreamBody)
    {
        var provider = CreateProvider();
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, upstreamBody);

        await InvokeTransformAsync(provider, context);

        var responseBody = await ReadBodyAsync(context.HttpContext.Response);
        Assert.Equal(StatusCodes.Status502BadGateway, context.HttpContext.Response.StatusCode);
        Assert.Equal(string.Empty, responseBody);
        Assert.Equal(0, context.HttpContext.Response.Headers.SetCookie.Count);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("past")]
    [InlineData("default")]
    public async Task Transform_WhenRefreshExpiresAtUnusable_Returns502WithoutTokens(string kind)
    {
        var logger = new ListLogger();
        var tokenJson = kind switch
        {
            "missing" => TokenJson(includeRefreshExpiresAt: false),
            "past" => TokenJson(refreshExpiresAt: DateTime.UtcNow.AddMinutes(-5)),
            _ => TokenJson(refreshExpiresAt: DateTime.MinValue)
        };
        var provider = CreateProvider(logger: logger);
        var context = CreateTransformContext("/api/v1/auth/login", HttpStatusCode.OK, tokenJson);

        await InvokeTransformAsync(provider, context);

        var responseBody = await ReadBodyAsync(context.HttpContext.Response);
        AssertGeneric502(context.HttpContext.Response, responseBody, "access-123", "refresh-456");
        var log = string.Join('\n', logger.Messages);
        Assert.DoesNotContain("access-123", log, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh-456", log, StringComparison.Ordinal);
        Assert.DoesNotContain(tokenJson, log, StringComparison.Ordinal);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("refresh-expiry", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/refresh")]
    public async Task Transform_WhenTokensValid_CookiesUseTokenExpiriesAndKeepFlags(string path)
    {
        var provider = CreateProvider();
        var context = CreateTransformContext(path, HttpStatusCode.OK, TokenJson());

        await InvokeTransformAsync(provider, context);

        var setCookieList = context.HttpContext.Response.Headers.SetCookie
            .Where(h => h != null)
            .Select(h => h!)
            .ToList();
        var access = Assert.Single(setCookieList, h => h.StartsWith("access_token=access-123", StringComparison.Ordinal));
        var refresh = Assert.Single(setCookieList, h => h.StartsWith("refresh_token=refresh-456", StringComparison.Ordinal));

        Assert.Contains(ExpiresAttribute(SampleAccessExpiresAt), access, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ExpiresAttribute(SampleRefreshExpiresAt), refresh, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ExpiresAttribute(SampleRefreshExpiresAt), access, StringComparison.OrdinalIgnoreCase);

        foreach (var header in new[] { access, refresh })
        {
            Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(HasCookiePath(access, "/"));
        Assert.True(HasCookiePath(refresh, "/api/v1/auth"));
        Assert.False(HasCookiePath(refresh, "/api/v1/auth/refresh"));
        Assert.Contains(setCookieList, h =>
            h.StartsWith("refresh_token=;", StringComparison.Ordinal)
            && HasCookiePath(h, "/"));
    }

    [Fact]
    public async Task Transform_WhenAccessPathConfigured_RefreshPathStaysAuthPrefix()
    {
        var settings = new CookieSettings { Path = "/api/v1" };
        var provider = CreateProvider(settings);
        var context = CreateTransformContext("/api/v1/auth/refresh", HttpStatusCode.OK, TokenJson());

        await InvokeTransformAsync(provider, context);

        var setCookieList = context.HttpContext.Response.Headers.SetCookie
            .Where(h => h != null)
            .Select(h => h!)
            .ToList();
        var access = Assert.Single(setCookieList, h => h.StartsWith("access_token=", StringComparison.Ordinal));
        var refresh = Assert.Single(setCookieList, h => h.StartsWith("refresh_token=refresh-456", StringComparison.Ordinal));

        Assert.True(HasCookiePath(access, "/api/v1"));
        Assert.True(HasCookiePath(refresh, "/api/v1/auth"));
    }

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/refresh")]
    public async Task Transform_WhenTokenResponse_ClearsLegacyRefreshCookieOnCookiePath(string path)
    {
        var settings = new CookieSettings { Path = "/legacy" };
        var provider = CreateProvider(settings);
        var context = CreateTransformContext(path, HttpStatusCode.OK, TokenJson());

        await InvokeTransformAsync(provider, context);

        var setCookieList = context.HttpContext.Response.Headers.SetCookie
            .Where(h => h != null)
            .Select(h => h!)
            .ToList();
        var legacy = Assert.Single(setCookieList, h =>
            h.StartsWith("refresh_token=;", StringComparison.Ordinal)
            && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));

        Assert.True(HasCookiePath(legacy, "/legacy"));
        Assert.False(HasCookiePath(legacy, "/"));
        Assert.Contains("httponly", legacy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", legacy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", legacy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Transform_WhenLogout401_ClearsNewAndLegacyRefreshCookies()
    {
        var settings = new CookieSettings { Path = "/legacy" };
        var provider = CreateProvider(settings);
        var context = CreateTransformContext("/api/v1/auth/logout", HttpStatusCode.Unauthorized, """{"detail":"no"}""");

        await InvokeTransformAsync(provider, context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.HttpContext.Response.StatusCode);
        var setCookieList = context.HttpContext.Response.Headers.SetCookie
            .Where(h => h != null)
            .Select(h => h!)
            .ToList();
        var refreshClears = setCookieList
            .Where(h => h.StartsWith("refresh_token=;", StringComparison.Ordinal)
                && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Contains(refreshClears, h => HasCookiePath(h, "/api/v1/auth"));
        Assert.Contains(refreshClears, h => HasCookiePath(h, "/legacy"));
        Assert.Contains(setCookieList, h => h.StartsWith("access_token=;", StringComparison.Ordinal) && HasCookiePath(h, "/legacy"));
    }

    [Fact]
    public async Task Transform_WhenLogout_ClearsRefreshCookieOnAuthPathAndLegacyRoot()
    {
        var settings = new CookieSettings { Path = "/api/v1" };
        var provider = CreateProvider(settings);
        var context = CreateTransformContext("/api/v1/auth/logout", HttpStatusCode.NoContent, null);

        await InvokeTransformAsync(provider, context);

        var setCookieList = context.HttpContext.Response.Headers.SetCookie
            .Where(h => h != null)
            .Select(h => h!)
            .ToList();
        var access = Assert.Single(setCookieList, h => h.StartsWith("access_token=;", StringComparison.Ordinal));
        var refreshClears = setCookieList
            .Where(h => h.StartsWith("refresh_token=;", StringComparison.Ordinal)
                && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(HasCookiePath(access, "/api/v1"));
        Assert.Contains(refreshClears, h => HasCookiePath(h, "/api/v1/auth"));
        Assert.Contains(refreshClears, h => HasCookiePath(h, "/api/v1"));
        Assert.DoesNotContain(refreshClears, h => HasCookiePath(h, "/api/v1/auth/refresh"));
        Assert.All(refreshClears, h =>
        {
            Assert.Contains("httponly", h, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", h, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=lax", h, StringComparison.OrdinalIgnoreCase);
        });
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

internal sealed class ListLogger : ILogger<AuthResponseTransformProvider>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public IEnumerable<string> Messages => Entries.Select(entry => entry.Message);

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
