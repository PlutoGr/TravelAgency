using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;
using TravelAgency.Gateway.Extensions;

namespace TravelAgency.Gateway.Tests.Extensions;

public class RateLimitingExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?>? overrides = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(overrides ?? [])
            .Build();

    [Fact]
    public void AddGatewayRateLimiting_WithDefaultConfiguration_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var ex = Record.Exception(() => services.AddGatewayRateLimiting(BuildConfig()));
        Assert.Null(ex);
    }

    [Fact]
    public void AddGatewayRateLimiting_RegistersRateLimiterOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGatewayRateLimiting(BuildConfig());

        // Act
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<RateLimiterOptions>>();

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void AddGatewayRateLimiting_WithCustomLimits_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["RateLimiting:General:Limit"] = "50",
            ["RateLimiting:General:PeriodSeconds"] = "10",
            ["RateLimiting:Auth:Limit"] = "3",
            ["RateLimiting:Auth:PeriodSeconds"] = "60",
            ["RateLimiting:Global:Limit"] = "500",
        });

        // Act & Assert
        var ex = Record.Exception(() => services.AddGatewayRateLimiting(config));
        Assert.Null(ex);
    }

    [Fact]
    public async Task UseGatewayRateLimiting_StartsWithoutThrowingExceptions()
    {
        // Arrange & Act
        using var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer()
                   .ConfigureServices(services =>
                   {
                       services.AddRouting();
                       services.AddGatewayRateLimiting(BuildConfig());
                   })
                   .Configure(app =>
                   {
                       app.UseRouting();
                       app.UseGatewayRateLimiting();
                   });
            })
            .StartAsync();

        // Assert — if we reach here the middleware pipeline assembled without errors
        Assert.NotNull(host);
    }

    [Fact]
    public void AddGatewayRateLimiting_RejectionStatusCodeIs429()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGatewayRateLimiting(BuildConfig());
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        // Assert
        Assert.Equal(429, options.RejectionStatusCode);
    }
}
