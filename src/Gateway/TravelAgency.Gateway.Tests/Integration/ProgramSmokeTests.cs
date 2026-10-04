using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("JwtSettings:SigningKey", "smoke-test-signing-key-at-least-32-chars!");
            builder.UseSetting("JwtSettings:Issuer", "smoke-issuer");
            builder.UseSetting("JwtSettings:Audience", "smoke-audience");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SigningKey"] = "smoke-test-signing-key-at-least-32-chars!",
                    ["JwtSettings:Issuer"] = "smoke-issuer",
                    ["JwtSettings:Audience"] = "smoke-audience",
                });
            });
        });
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
