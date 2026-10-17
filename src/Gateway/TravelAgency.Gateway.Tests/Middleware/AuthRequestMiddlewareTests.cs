using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;
using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.Middleware;

namespace TravelAgency.Gateway.Tests.Middleware;

public class AuthRequestMiddlewareTests
{
    [Theory]
    [InlineData("/api/v1/auth/refresh")]
    [InlineData("/api/v1/auth/logout")]
    public async Task Invoke_WhenTwoRefreshCookiesAndEmptyBody_InjectsFirstOccurrence(string path)
    {
        string? captured = null;
        var middleware = new AuthRequestMiddleware(
            context =>
            {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                captured = reader.ReadToEnd();
                return Task.CompletedTask;
            },
            Options.Create(new CookieSettings()),
            Options.Create(new AuthRouteSettings()));

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.ContentLength = 0;
        context.Request.Headers.Cookie = "refresh_token=from-auth-path; refresh_token=from-legacy-path";

        await middleware.InvokeAsync(context);

        Assert.NotNull(captured);
        using var doc = JsonDocument.Parse(captured);
        Assert.Equal("from-auth-path", doc.RootElement.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Invoke_WhenBasePathChanges_MatchesThatPrefixOnly()
    {
        var called = false;
        var middleware = new AuthRequestMiddleware(
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            Options.Create(new CookieSettings()),
            Options.Create(new AuthRouteSettings { BasePath = "/api/v2/auth" }));

        var oldPath = new DefaultHttpContext();
        oldPath.Request.Method = HttpMethods.Post;
        oldPath.Request.Path = "/api/v1/auth/refresh";
        oldPath.Request.ContentLength = 0;
        oldPath.Request.Headers.Cookie = "refresh_token=old";
        await middleware.InvokeAsync(oldPath);
        Assert.Equal(0, oldPath.Request.ContentLength ?? 0);

        var newPath = new DefaultHttpContext();
        newPath.Request.Method = HttpMethods.Post;
        newPath.Request.Path = "/api/v2/auth/refresh";
        newPath.Request.ContentLength = 0;
        newPath.Request.Headers.Cookie = "refresh_token=from-auth-path; refresh_token=from-legacy-path";
        await middleware.InvokeAsync(newPath);

        Assert.True(called);
        newPath.Request.Body.Position = 0;
        using var reader = new StreamReader(newPath.Request.Body);
        using var doc = JsonDocument.Parse(await reader.ReadToEndAsync());
        Assert.Equal("from-auth-path", doc.RootElement.GetProperty("refreshToken").GetString());
    }
}
