using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Infrastructure.GrpcClients;

public class IdentityGrpcClient : IIdentityGrpcClient
{
    private readonly IdentityGrpc.IdentityGrpcClient _client;

    public IdentityGrpcClient(IdentityGrpc.IdentityGrpcClient client)
    {
        _client = client;
    }

    public async Task<UserSummaryDto> GetUserSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var request = new GetUserSummaryRequest { UserId = userId.ToString() };
        var response = await _client.GetUserSummaryAsync(request, cancellationToken: ct);

        return new UserSummaryDto(
            UserId: Guid.Parse(response.UserId),
            Email: response.Email,
            FirstName: response.FirstName,
            LastName: response.LastName,
            Role: response.Role);
    }
}
