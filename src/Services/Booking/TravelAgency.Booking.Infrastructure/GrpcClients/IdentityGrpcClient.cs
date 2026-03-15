using Microsoft.Extensions.Configuration;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Contracts.Grpc.Identity;

namespace TravelAgency.Booking.Infrastructure.GrpcClients;

public class IdentityGrpcClient : IIdentityGrpcClient
{
    private readonly IdentityGrpc.IdentityGrpcClient _client;
    private readonly IConfiguration _configuration;

    public IdentityGrpcClient(IdentityGrpc.IdentityGrpcClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new GetUserSummaryRequest { UserId = userId.ToString() };
            var callOptions = CreateCallOptions(ct);
            var response = await _client.GetUserSummaryAsync(request, callOptions);

            return new UserSummary(
                response.UserId,
                response.Email,
                response.FirstName ?? string.Empty,
                response.LastName ?? string.Empty,
                response.Role ?? string.Empty);
        }
        catch (Grpc.Core.RpcException)
        {
            return null;
        }
    }

    private Grpc.Core.CallOptions CreateCallOptions(CancellationToken ct = default)
    {
        var token = _configuration["GrpcSettings:InternalServiceToken"];
        if (string.IsNullOrEmpty(token))
            return new Grpc.Core.CallOptions(cancellationToken: ct);

        var metadata = new Grpc.Core.Metadata
        {
            { "x-internal-auth", token }
        };
        return new Grpc.Core.CallOptions(metadata, deadline: null, ct);
    }
}
