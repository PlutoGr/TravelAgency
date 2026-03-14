using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;

namespace TravelAgency.Identity.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var value = FindClaim(ClaimTypes.NameIdentifier)
                        ?? FindClaim(JwtRegisteredClaimNames.Sub);
            if (value is null)
                throw new UnauthorizedException("User is not authenticated.");

            return Guid.TryParse(value, out var guid)
                ? guid
                : throw new UnauthorizedException("Invalid user identity claim in token.");
        }
    }

    public string Email =>
        FindClaim(JwtRegisteredClaimNames.Email)
        ?? FindClaim(ClaimTypes.Email)
        ?? string.Empty;

    public string Role =>
        FindClaim(ClaimTypes.Role)
        ?? FindClaim("role")
        ?? string.Empty;

    private string? FindClaim(string type) =>
        httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
}
