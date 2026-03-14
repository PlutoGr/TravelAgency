using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Application.Interfaces;

public interface IJwtTokenService
{
    AuthTokensDto GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
