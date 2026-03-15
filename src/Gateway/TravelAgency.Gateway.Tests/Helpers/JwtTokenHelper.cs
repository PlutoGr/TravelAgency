using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Gateway.Tests.Helpers;

/// <summary>
/// Generates JWT tokens for Gateway integration tests.
/// Uses same Issuer, Audience, and SigningKey as GatewayWebApplicationFactory.
/// </summary>
public static class JwtTokenHelper
{
    /// <summary>
    /// Generates a valid JWT for the Gateway with Client role.
    /// </summary>
    public static string GenerateToken(Guid? userId = null, string role = AppRoles.Client)
    {
        var id = userId ?? Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GatewayWebApplicationFactory.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: GatewayWebApplicationFactory.JwtIssuer,
            audience: GatewayWebApplicationFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
