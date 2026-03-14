using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Abstractions;

public interface IIdentityGrpcClient
{
    Task<UserSummaryDto> GetUserSummaryAsync(Guid userId, CancellationToken ct = default);
}
