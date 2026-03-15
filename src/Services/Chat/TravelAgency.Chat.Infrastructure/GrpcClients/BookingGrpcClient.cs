using Microsoft.Extensions.Configuration;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Contracts.Grpc.Booking;

namespace TravelAgency.Chat.Infrastructure.GrpcClients;

/// <summary>
/// gRPC client for validating booking access via the Booking service.
/// Uses x-internal-auth header for service-to-service authentication.
/// </summary>
public sealed class BookingGrpcClient : IBookingGrpcClient
{
    private readonly BookingService.BookingServiceClient _client;
    private readonly IConfiguration _configuration;

    public BookingGrpcClient(BookingService.BookingServiceClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<bool> ValidateBookingAccessAsync(Guid bookingId, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new ValidateBookingAccessRequest
            {
                BookingId = bookingId.ToString(),
                UserId = userId.ToString()
            };
            var callOptions = CreateCallOptions(ct);
            var response = await _client.ValidateBookingAccessAsync(request, callOptions);
            return response.HasAccess;
        }
        catch (Grpc.Core.RpcException)
        {
            return false;
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
