using Grpc.Core;
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
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC call to Identity service failed for user {UserId}", userId);
            return null;
        }
    }

    public async Task<UserRoleLookup> GetUserRoleAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new GetUserSummaryRequest { UserId = userId.ToString() };
            var callOptions = _callOptionsFactory.Create(ct);
            var response = await _client.GetUserSummaryAsync(request, callOptions);
            return new UserRoleLookup(true, response.Role ?? string.Empty);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return new UserRoleLookup(false, string.Empty);
        }
    }

    public async Task<IdentityCardProfileRead> ReadCardProfileAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new GetUserSummaryRequest { UserId = userId.ToString() };
            var response = await _client.GetUserSummaryAsync(request, _callOptionsFactory.Create(ct));
            var avatar = string.IsNullOrWhiteSpace(response.AvatarUrl) ? null : response.AvatarUrl;
            return new IdentityCardProfileRead(
                IdentityReadKind.Found,
                new IdentityCardProfile(
                    response.Email ?? string.Empty,
                    response.DisplayName ?? string.Empty,
                    response.Role ?? string.Empty,
                    avatar));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return new IdentityCardProfileRead(IdentityReadKind.NotFound, null);
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(
                "gRPC GetUserSummary failed with {StatusCode} for user {UserId}",
                ex.StatusCode,
                userId);
            return new IdentityCardProfileRead(IdentityReadKind.Unavailable, null);
        }
    }

    public async Task<UserPhoneRead> ReadUserPhoneAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new GetUserPhoneRequest { UserId = userId.ToString() };
            var response = await _client.GetUserPhoneAsync(request, _callOptionsFactory.Create(ct));
            return new UserPhoneRead(IdentityReadKind.Found, response.Phone ?? string.Empty);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return new UserPhoneRead(IdentityReadKind.NotFound, string.Empty);
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(
                "gRPC GetUserPhone failed with {StatusCode} for user {UserId}",
                ex.StatusCode,
                userId);
            return new UserPhoneRead(IdentityReadKind.Unavailable, string.Empty);
        }
    }
}
