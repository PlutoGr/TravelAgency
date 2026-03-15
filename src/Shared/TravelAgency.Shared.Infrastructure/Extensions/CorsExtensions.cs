using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TravelAgency.Shared.Infrastructure.Extensions;

/// <summary>
/// CORS setup with whitelist validation. AllowCredentials requires explicit origins;
/// wildcard (*) is not allowed for security.
/// </summary>
public static class CorsExtensions
{
    public const string GatewayCorsPolicyName = "GatewayPolicy";

    private const string CorsSection = "Cors";
    private const string AllowedOriginsKey = "AllowedOrigins";
    private const string AllowedMethodsKey = "AllowedMethods";
    private const string AllowedHeadersKey = "AllowedHeaders";

    /// <summary>
    /// Adds CORS with default policy (AllowAnyHeader, AllowAnyMethod, AllowCredentials).
    /// Uses Cors:AllowedOrigins from configuration. Validates against wildcard.
    /// </summary>
    public static IServiceCollection AddSharedCors(
        this IServiceCollection services,
        IConfiguration configuration,
        string[]? defaultOrigins = null)
    {
        var origins = GetOrigins(configuration, defaultOrigins);
        ValidateOrigins(origins);

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds CORS with named policy for Gateway (explicit methods and headers from config).
    /// Uses Cors:AllowedOrigins, Cors:AllowedMethods, Cors:AllowedHeaders.
    /// </summary>
    public static IServiceCollection AddSharedCorsForGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(CorsSection);
        var origins = section.GetSection(AllowedOriginsKey).Get<string[]>() ?? Array.Empty<string>();

        if (origins.Length == 0)
            origins = ["http://localhost:3000", "http://localhost:5173"];

        ValidateOrigins(origins);

        var methods = section.GetSection(AllowedMethodsKey).Get<string[]>();
        var headers = section.GetSection(AllowedHeadersKey).Get<string[]>();

        services.AddCors(options =>
        {
            options.AddPolicy(GatewayCorsPolicyName, policy =>
            {
                var builder = policy.WithOrigins(origins).AllowCredentials();
                builder = methods is { Length: > 0 } ? builder.WithMethods(methods) : builder.AllowAnyMethod();
                builder = headers is { Length: > 0 } ? builder.WithHeaders(headers) : builder.AllowAnyHeader();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSharedCors(this IApplicationBuilder app, string? policyName = null)
    {
        if (policyName is null)
            app.UseCors();
        else
            app.UseCors(policyName);
        return app;
    }

    private static string[] GetOrigins(IConfiguration configuration, string[]? defaultOrigins)
    {
        var origins = configuration.GetSection(CorsSection).GetSection(AllowedOriginsKey).Get<string[]>();

        if (origins is null or { Length: 0 })
            origins = defaultOrigins ?? ["http://localhost:3000", "http://localhost:5173"];

        return origins;
    }

    private static void ValidateOrigins(string[] origins)
    {
        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                "CORS AllowedOrigins must contain at least one origin. Configure at least one value in Cors:AllowedOrigins.");
        }

        if (origins.Any(o => o == "*"))
        {
            throw new InvalidOperationException(
                "CORS AllowedOrigins cannot use wildcard (*) when AllowCredentials is true. Use explicit origin(s).");
        }
    }
}
