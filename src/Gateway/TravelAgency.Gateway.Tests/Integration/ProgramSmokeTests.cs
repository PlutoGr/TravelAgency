using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TravelAgency.Gateway.Tests.Integration;

/// <summary>
/// Verifies that the gateway application starts up without errors and its
/// infrastructure endpoints respond correctly.
/// </summary>
public class ProgramSmokeTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        // Environment variables are read during WebApplication.CreateBuilder(), before
        // any service registration runs, so this is the reliable way to inject test config
        // into a Minimal API entry point with WebApplicationFactory.
        Environment.SetEnvironmentVariable("JwtSettings__SigningKey", "smoke-test-signing-key-at-least-32-chars!");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "smoke-issuer");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "smoke-audience");

        return new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task Application_StartsSuccessfully_HealthLiveEndpointReturns200()
    {
        // Arrange
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Application_StartsSuccessfully_HealthReadyEndpointResponds()
    {
        // Arrange — /health/ready runs "ready"-tagged checks; with no downstream endpoints
        // configured it returns 200 (no checks to fail).
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Application_SecurityHeaders_ArePresent()
    {
        // Arrange
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var xCto));
        Assert.Equal("nosniff", xCto.Single());

        Assert.True(response.Headers.TryGetValues("X-Frame-Options", out var xFrame));
        Assert.Equal("DENY", xFrame.Single());

        Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var referrer));
        Assert.Equal("no-referrer", referrer.Single());
    }
}
