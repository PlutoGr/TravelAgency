namespace TravelAgency.Gateway.Extensions;

public static class CorsExtensions
{
    public const string GatewayCorsPolicyName = "GatewayPolicy";

    public static IServiceCollection AddGatewayCors(
        this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var allowedMethods = corsSection.GetSection("AllowedMethods").Get<string[]>() ?? Array.Empty<string>();
        var allowedHeaders = corsSection.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(GatewayCorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .WithMethods(allowedMethods)
                      .WithHeaders(allowedHeaders)
                      .AllowCredentials();
            });
        });

        return services;
    }
}
