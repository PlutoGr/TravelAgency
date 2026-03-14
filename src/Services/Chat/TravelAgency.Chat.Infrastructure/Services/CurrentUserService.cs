using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TravelAgency.Chat.Application.Abstractions;

namespace TravelAgency.Chat.Infrastructure.Services;

/// <summary>
/// Reads current user identity from JWT claims in the HTTP context.
/// Supports claim names: "sub" or "userId" for UserId; "role" or "Role" for Role.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("userId");

            if (claim is null || !Guid.TryParse(claim.Value, out var userId))
                return Guid.Empty;

            return userId;
        }
    }

    public string Role =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value
        ?? _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value
        ?? _httpContextAccessor.HttpContext?.User.FindFirst("Role")?.Value
        ?? string.Empty;

    public string DisplayName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
                return "User " + UserId.ToString();

            var name = user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("name")?.Value
                ?? user.FindFirst("preferred_username")?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value;

            return !string.IsNullOrWhiteSpace(name) ? name : "User " + UserId.ToString();
        }
    }
}
