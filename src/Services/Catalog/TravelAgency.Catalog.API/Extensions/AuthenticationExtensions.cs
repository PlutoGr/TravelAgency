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
        var jwtSection = configuration.GetSection("JwtSettings");
        var jwtSettings = jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings must be configured.");

        var signingKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
            ?? jwtSection["SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException(
                "JWT SigningKey must be configured via environment variable JWT_SIGNING_KEY or JwtSettings__SigningKey.");

        if (signingKey.Length < 32)
            throw new InvalidOperationException("JWT SigningKey must be at least 32 characters");

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
