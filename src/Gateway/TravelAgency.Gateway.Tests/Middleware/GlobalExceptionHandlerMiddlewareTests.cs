using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TravelAgency.Gateway.Middleware;
using Xunit;

namespace TravelAgency.Gateway.Tests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static async Task<IHost> CreateHostAsync(
        bool isDevelopment,
        RequestDelegate next)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);

        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(env);
                        services.AddLogging(builder => builder.AddConsole());
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
                        app.Run(next);
                    });
            })
            .StartAsync();
        return host;
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_Returns500_ApplicationProblemJson_AndProblemDetailsWithTraceId()
    {
        // Arrange
        var exceptionMessage = "Something broke.";
        RequestDelegate next = _ => throw new InvalidOperationException(exceptionMessage);
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal(exceptionMessage, root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("instance", out var instance));
        Assert.True(root.TryGetProperty("traceId", out var traceIdExt));
        Assert.Equal(JsonValueKind.String, traceIdExt.ValueKind);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_InDevelopment_DetailEqualsExceptionMessage()
    {
        // Arrange
        var message = "Dev-only detail.";
        RequestDelegate next = _ => throw new ArgumentException(message);
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.Equal(message, doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_InProduction_DetailIsGenericMessage()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("Secret internal detail.");
        using var host = await CreateHostAsync(isDevelopment: false, next);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("An unexpected error occurred.", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ProblemDetailsContainsStatusTitleDetailInstanceAndTraceIdInExtensions()
    {
        // Arrange
        RequestDelegate next = _ => throw new Exception("Any error.");
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/path");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.NotNull(root.GetProperty("detail").GetString());
        Assert.Equal("/path", root.GetProperty("instance").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.Equal(JsonValueKind.String, traceId.ValueKind);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasAlreadyStarted_DoesNotWriteBody_AndDoesNotThrow()
    {
        // Arrange
        const string bodySent = "already-sent";
        RequestDelegate next = async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(bodySent);
            throw new InvalidOperationException("Too late.");
        };
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(bodySent, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Internal Server Error", body);
        Assert.NotEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
