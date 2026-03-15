namespace TravelAgency.Booking.Application.Abstractions;

public record UserSummary(string UserId, string Email, string FirstName, string LastName, string Role);

public interface IIdentityGrpcClient
{
    Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken ct = default);
}
