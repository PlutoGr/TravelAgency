using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TravelAgency.Booking.Application.Abstractions;

namespace TravelAgency.Booking.Infrastructure.Services;

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
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub");

            if (claim is null || !Guid.TryParse(claim.Value, out var userId))
                return Guid.Empty;

            return userId;
        }
    }

    public string Role =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value
        ?? _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value
        ?? string.Empty;
}
