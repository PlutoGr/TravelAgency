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
        if (!TryParseIds(request.BookingId, request.UserId, out var bookingId, out var userId))
            return Denied();

        var booking = await _bookingRepository.GetByIdAsync(bookingId, context.CancellationToken);
        if (booking == null)
            return Denied();

        if (booking.ClientId == userId)
            return new ValidateBookingAccessResponse { HasAccess = true, CanWrite = true };

        var userSummary = await _identityGrpcClient.GetUserSummaryAsync(userId, context.CancellationToken);
        if (userSummary == null)
            return Denied();

        if (userSummary.Role == AppRoles.Admin)
            return new ValidateBookingAccessResponse { HasAccess = true, CanWrite = true };

        if (userSummary.Role == AppRoles.Manager)
        {
            return new ValidateBookingAccessResponse
            {
                HasAccess = true,
                CanWrite = booking.AssignedManagerId == userId
            };
        }

        return Denied();
    }

    public override async Task<GetBookingAssignmentResponse> GetBookingAssignment(
        GetBookingAssignmentRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.BookingId, out var bookingId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "booking_id is invalid."));

        var booking = await _bookingRepository.GetByIdAsync(bookingId, context.CancellationToken);
        if (booking == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Booking was not found."));

        var response = new GetBookingAssignmentResponse
        {
            ClientId = booking.ClientId.ToString(),
            AssignedManagerId = booking.AssignedManagerId?.ToString() ?? string.Empty
        };

        var seen = new HashSet<Guid>();
        foreach (var entry in booking.AssignmentHistory.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id))
        {
            AddPrevious(response, seen, booking.AssignedManagerId, entry.PreviousManagerId);
            AddPrevious(response, seen, booking.AssignedManagerId, entry.NewManagerId);
        }

        return response;
    }

    private static void AddPrevious(
        GetBookingAssignmentResponse response,
        HashSet<Guid> seen,
        Guid? currentManagerId,
        Guid? managerId)
    {
        if (managerId is not Guid id || id == Guid.Empty || id == currentManagerId)
            return;

        if (seen.Add(id))
            response.PreviousManagerIds.Add(id.ToString());
    }

    private static bool TryParseIds(string bookingId, string userId, out Guid parsedBookingId, out Guid parsedUserId)
    {
        var bookingOk = Guid.TryParse(bookingId, out parsedBookingId);
        var userOk = Guid.TryParse(userId, out parsedUserId);
        return bookingOk && userOk;
    }

    private static ValidateBookingAccessResponse Denied() =>
        new() { HasAccess = false, CanWrite = false };
}
