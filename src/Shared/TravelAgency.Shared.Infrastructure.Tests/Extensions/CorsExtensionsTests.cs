using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Shared.Infrastructure.Tests.Extensions;

public class CorsExtensionsTests
{
    [Fact]
    public void AddSharedCors_WithConfigOrigins_RegistersCorsWithOrigins()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://example.com",
                ["Cors:AllowedOrigins:1"] = "https://other.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSharedCors(config);

        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;

        var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain("https://example.com");
        policy.Origins.Should().Contain("https://other.com");
        policy.AllowAnyHeader.Should().BeTrue();
        policy.AllowAnyMethod.Should().BeTrue();
        policy.SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void AddSharedCors_WithDefaultOrigins_WhenConfigEmpty_UsesDefaultOrigins()
    {
        var config = new ConfigurationBuilder().Build();
        var defaultOrigins = new[] { "https://fallback.local" };

        var services = new ServiceCollection();
        services.AddSharedCors(config, defaultOrigins);

        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain("https://fallback.local");
    }

    [Fact]
    public void AddSharedCors_WithEmptyOrigins_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();

        var act = () => services.AddSharedCors(config, defaultOrigins: Array.Empty<string>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CORS AllowedOrigins must contain at least one origin*");
    }

    [Fact]
    public void AddSharedCors_WithWildcardOrigin_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "*"
            })
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddSharedCors(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*wildcard (*)*");
    }

    [Fact]
    public void AddSharedCorsForGateway_WithConfig_RegistersGatewayPolicy()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://gateway.local",
                ["Cors:AllowedMethods:0"] = "GET",
                ["Cors:AllowedMethods:1"] = "POST",
                ["Cors:AllowedHeaders:0"] = "Authorization"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSharedCorsForGateway(config);

        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(CorsExtensions.GatewayCorsPolicyName);

        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain("https://gateway.local");
        policy.Methods.Should().Contain("GET");
        policy.Methods.Should().Contain("POST");
        policy.Headers.Should().Contain("Authorization");
        policy.SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void AddSharedCorsForGateway_WithEmptyConfig_UsesDefaultOrigins()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSharedCorsForGateway(config);

        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(CorsExtensions.GatewayCorsPolicyName);

        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain("http://localhost:3000");
        policy.Origins.Should().Contain("http://localhost:5173");
    }

    [Fact]
    public void AddSharedCorsForGateway_WithWildcardOrigin_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "*"
            })
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddSharedCorsForGateway(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*wildcard*");
    }

    [Fact]
    public void UseSharedCors_WithNullPolicyName_DoesNotThrow()
    {
        var app = WebApplication.CreateBuilder().Build();

        var act = () => app.UseSharedCors(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void UseSharedCors_WithPolicyName_DoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddCors(o => o.AddPolicy("Test", p => p.WithOrigins("https://test.com")));
        var app = builder.Build();

        var act = () => app.UseSharedCors("Test");

        act.Should().NotThrow();
    }
}
