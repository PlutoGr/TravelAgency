using Microsoft.Extensions.Logging;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Contracts.Grpc.Identity;
using TravelAgency.Shared.Infrastructure.GrpcServices;

namespace TravelAgency.Booking.Infrastructure.GrpcClients;

public class IdentityGrpcClient : IIdentityGrpcClient
{
    private readonly IdentityGrpc.IdentityGrpcClient _client;
    private readonly IGrpcAuthCallOptionsFactory _callOptionsFactory;
    private readonly ILogger<IdentityGrpcClient> _logger;

    public IdentityGrpcClient(IdentityGrpc.IdentityGrpcClient client, IGrpcAuthCallOptionsFactory callOptionsFactory, ILogger<IdentityGrpcClient> logger)
    {
        _client = client;
        _callOptionsFactory = callOptionsFactory;
        _logger = logger;
    }

    public async Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new GetUserSummaryRequest { UserId = userId.ToString() };
            var callOptions = _callOptionsFactory.Create(ct);
            var response = await _client.GetUserSummaryAsync(request, callOptions);

            return new UserSummary(
                response.UserId,
                response.Email,
                response.FirstName ?? string.Empty,
                response.LastName ?? string.Empty,
                response.Role ?? string.Empty);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC call to Identity service failed for user {UserId}", userId);
            return null;
        }
    }
}
