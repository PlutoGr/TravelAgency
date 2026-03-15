using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TravelAgency.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for JWT Bearer authentication shared across services.
/// </summary>
public static class JwtAuthenticationExtensions
{
    private const int MinSigningKeyLength = 32;

    /// <summary>
    /// Adds JWT Bearer authentication using signing key from JWT_SIGNING_KEY env var or JwtSettings:SigningKey.
    /// Validates signing key: non-empty, minimum 32 characters.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration containing JwtSettings (Issuer, Audience, ValidateLifetime).</param>
    /// <param name="configureOptions">Optional additional configuration for JwtBearerOptions (e.g. SignalR OnMessageReceived).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when signing key is missing, empty, or shorter than 32 characters.</exception>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureOptions = null)
    {
        var signingKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
            ?? configuration["JwtSettings:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException(
                "JWT SigningKey must be configured via environment variable JWT_SIGNING_KEY or JwtSettings:SigningKey.");

        if (signingKey.Length < MinSigningKeyLength)
            throw new InvalidOperationException("JWT SigningKey must be at least 32 characters.");

        var jwtSection = configuration.GetSection("JwtSettings");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = jwtSection.GetValue<bool>("ValidateLifetime", true),
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
            configureOptions?.Invoke(options);
        });

        return services;
    }
}
