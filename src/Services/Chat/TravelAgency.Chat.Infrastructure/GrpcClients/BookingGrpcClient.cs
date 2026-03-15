using Microsoft.Extensions.Logging;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Contracts.Grpc.Booking;
using TravelAgency.Shared.Infrastructure.GrpcServices;

namespace TravelAgency.Chat.Infrastructure.GrpcClients;

/// <summary>
/// gRPC client for validating booking access via the Booking service.
/// Uses x-internal-auth header for service-to-service authentication.
/// </summary>
public sealed class BookingGrpcClient : IBookingGrpcClient
{
    private readonly BookingService.BookingServiceClient _client;
    private readonly IGrpcAuthCallOptionsFactory _callOptionsFactory;
    private readonly ILogger<BookingGrpcClient> _logger;

    public BookingGrpcClient(BookingService.BookingServiceClient client, IGrpcAuthCallOptionsFactory callOptionsFactory, ILogger<BookingGrpcClient> logger)
    {
        _client = client;
        _callOptionsFactory = callOptionsFactory;
        _logger = logger;
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
            var callOptions = _callOptionsFactory.Create(ct);
            var response = await _client.ValidateBookingAccessAsync(request, callOptions);
            return response.HasAccess;
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC call to Booking service failed for booking {BookingId}, user {UserId}", bookingId, userId);
            return false;
        }
    }
}
