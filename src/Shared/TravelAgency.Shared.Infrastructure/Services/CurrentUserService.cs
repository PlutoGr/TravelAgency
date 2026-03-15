using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Shared.Infrastructure.Services;

/// <summary>
/// Reads current user identity from JWT claims in the HTTP context.
/// Lenient mode: returns Guid.Empty and empty strings when not authenticated.
/// Services that require auth (e.g. Identity) should check <see cref="ICurrentUserService.IsAuthenticated"/>
/// and throw UnauthorizedException when false.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var value = FindClaim(ClaimTypes.NameIdentifier)
                ?? FindClaim("sub")
                ?? FindClaim("userId");
            if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var guid))
                return Guid.Empty;
            return guid;
        }
    }

    public string Email =>
        FindClaim(ClaimTypes.Email)
        ?? FindClaim("email")
        ?? string.Empty;

    public string Role =>
        FindClaim(ClaimTypes.Role)
        ?? FindClaim("role")
        ?? FindClaim("Role")
        ?? string.Empty;

    public string DisplayName
    {
        get
        {
            var name = FindClaim(ClaimTypes.Name)
                ?? FindClaim("name")
                ?? FindClaim("preferred_username")
                ?? FindClaim(ClaimTypes.Email)
                ?? FindClaim("email");
            return !string.IsNullOrWhiteSpace(name) ? name : "User " + UserId.ToString();
        }
    }

    private string? FindClaim(string type) =>
        httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
}
