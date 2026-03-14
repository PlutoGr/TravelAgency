using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Chat.IntegrationTests.Helpers;

/// <summary>
/// Generates JWT tokens for Chat API integration tests.
/// Uses same Issuer, Audience, and SigningKey as Chat appsettings.json JwtSettings.
/// </summary>
public static class JwtTokenHelper
{
    private const string Issuer = "travel-agency";
    private const string Audience = "travel-agency-users";
    private const string SigningKey = "CHANGE_ME_32_CHARACTERS_MINIMUM_KEY";

    /// <summary>
    /// Generates a valid JWT for the Chat API with Client role.
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
