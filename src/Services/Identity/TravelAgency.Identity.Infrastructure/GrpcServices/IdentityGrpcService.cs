using Grpc.Core;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Infrastructure.GrpcServices;

public sealed class IdentityGrpcService(IUserRepository userRepository)
    : IdentityGrpc.IdentityGrpcBase
{
    public override async Task<UserSummaryResponse> GetUserSummary(
        GetUserSummaryRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));

        var user = await userRepository.GetByIdAsync(userId, context.CancellationToken);

        if (user is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User '{request.UserId}' not found."));

        return new UserSummaryResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToRoleString()
        };
    }
}
