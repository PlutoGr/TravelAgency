using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TravelAgency.Catalog.Application.Settings;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Catalog.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCatalogAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings must be configured.");

        // Prefer JWT_SIGNING_KEY env var; fallback to config. Production must use env/secrets - do NOT commit real secrets.
        var signingKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") ?? jwtSettings.SigningKey;

        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException(
                "JWT SigningKey must be configured via environment variable JWT_SIGNING_KEY or JwtSettings:SigningKey.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(signingKey)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ManagerOrAdmin", policy =>
                policy.RequireRole(AppRoles.Manager, AppRoles.Admin));
        });

        return services;
    }
}
