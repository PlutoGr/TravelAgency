using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TravelAgency.Gateway.Tests.Helpers;

/// <summary>
/// WebApplicationFactory for Gateway integration tests with config overrides for
/// ReverseProxy clusters and HealthCheckEndpoints to point at a mock backend.
/// </summary>
public class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestSigningKey = "YarpTestSigningKeyWithAtLeast32Chars!";
    private const string TestIssuer = "YarpTestIssuer";
    private const string TestAudience = "YarpTestAudience";

    private readonly string _mockBackendUrl;

    public GatewayWebApplicationFactory(string mockBackendUrl)
    {
        _mockBackendUrl = mockBackendUrl.TrimEnd('/');
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("JwtSettings__SigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", TestIssuer);
        Environment.SetEnvironmentVariable("JwtSettings__Audience", TestAudience);

        // Rate limits are read eagerly in Program.cs (AddGatewayRateLimiting) before
        // ConfigureAppConfiguration overrides below are applied, so the in-memory values
        // alone are not seen and appsettings.json (Auth:Limit = 5/s) wins, causing 429s.
        // Environment variables are read during WebApplication.CreateBuilder(), so set them here.
        Environment.SetEnvironmentVariable("RateLimiting__Auth__Disabled", "true");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__Limit", "1000");
        Environment.SetEnvironmentVariable("RateLimiting__Global__Limit", "2000");

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var mockHealthUrl = $"{_mockBackendUrl}/health/live";
            var testSettings = new Dictionary<string, string?>
            {
                ["Cookie:Secure"] = "false",
                ["RateLimiting:Auth:Limit"] = "1000",
                ["RateLimiting:Auth:PeriodSeconds"] = "1",
                ["RateLimiting:Global:Limit"] = "2000",
                ["ReverseProxy:Clusters:identity-cluster:Destinations:destination1:Address"] = _mockBackendUrl,
                ["ReverseProxy:Clusters:catalog-cluster:Destinations:destination1:Address"] = _mockBackendUrl,
                ["ReverseProxy:Clusters:booking-cluster:Destinations:destination1:Address"] = _mockBackendUrl,
                ["ReverseProxy:Clusters:chat-cluster:Destinations:destination1:Address"] = _mockBackendUrl,
                ["ReverseProxy:Clusters:media-cluster:Destinations:destination1:Address"] = _mockBackendUrl,
                ["HealthCheckEndpoints:Identity"] = mockHealthUrl,
                ["HealthCheckEndpoints:Catalog"] = mockHealthUrl,
                ["HealthCheckEndpoints:Booking"] = mockHealthUrl,
                ["HealthCheckEndpoints:Chat"] = mockHealthUrl,
                ["HealthCheckEndpoints:Media"] = mockHealthUrl,
                ["JwtSettings:SigningKey"] = TestSigningKey,
                ["JwtSettings:Issuer"] = TestIssuer,
                ["JwtSettings:Audience"] = TestAudience,
            };
            config.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ITransformProvider, SelectedRouteHeaderTransformProvider>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
        });
    }

    /// <summary>
    /// Issuer used by JwtTokenHelper for this factory.
    /// </summary>
    public static string JwtIssuer => TestIssuer;

    /// <summary>
    /// Audience used by JwtTokenHelper for this factory.
    /// </summary>
    public static string JwtAudience => TestAudience;

    /// <summary>
    /// Signing key used by JwtTokenHelper for this factory.
    /// </summary>
    public static string JwtSigningKey => TestSigningKey;
}
