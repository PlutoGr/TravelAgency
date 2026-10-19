namespace TravelAgency.Chat.Application.Abstractions;

/// <summary>
/// gRPC client for validating booking access via the Booking service.
/// </summary>
public interface IBookingGrpcClient
{
    /// <summary>
    /// Validates whether the specified user has access to the booking.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user has access, false otherwise.</returns>
    Task<bool> ValidateBookingAccessAsync(Guid bookingId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Whether the user may send messages in the booking.
    /// Owning client, responsible manager and admin may write. Other managers may only read.
    /// </summary>
    Task<bool> CanWriteBookingAsync(Guid bookingId, Guid userId, CancellationToken ct = default);
}
