using Grpc.Core;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Contracts.Grpc.Booking;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Infrastructure.GrpcServices;

/// <summary>
/// gRPC service to validate booking access for internal service-to-service calls (e.g. Chat).
/// </summary>
public sealed class BookingGrpcService : BookingService.BookingServiceBase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IIdentityGrpcClient _identityGrpcClient;

    public BookingGrpcService(IBookingRepository bookingRepository, IIdentityGrpcClient identityGrpcClient)
    {
        _bookingRepository = bookingRepository;
        _identityGrpcClient = identityGrpcClient;
    }

    public override async Task<ValidateBookingAccessResponse> ValidateBookingAccess(
        ValidateBookingAccessRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.BookingId, out var bookingId) ||
            !Guid.TryParse(request.UserId, out var userId))
        {
            return new ValidateBookingAccessResponse { HasAccess = false };
        }

        var booking = await _bookingRepository.GetByIdAsync(bookingId, context.CancellationToken);
        if (booking == null)
            return new ValidateBookingAccessResponse { HasAccess = false };

        if (booking.ClientId == userId)
            return new ValidateBookingAccessResponse { HasAccess = true };

        var userSummary = await _identityGrpcClient.GetUserSummaryAsync(userId, context.CancellationToken);
        if (userSummary == null)
            return new ValidateBookingAccessResponse { HasAccess = false };

        var hasAccess = userSummary.Role is AppRoles.Manager or AppRoles.Admin;
        return new ValidateBookingAccessResponse { HasAccess = hasAccess };
    }
}
