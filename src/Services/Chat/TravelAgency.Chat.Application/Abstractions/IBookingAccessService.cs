namespace TravelAgency.Chat.Application.Abstractions;

/// <summary>
/// Verifies whether the current user has access to a given booking.
/// Uses the Booking service API with forwarded JWT for authorization.
/// </summary>
public interface IBookingAccessService
{
    /// <summary>
    /// Checks if the current user can access the specified booking.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="authorizationHeader">Optional Authorization header (e.g. from SignalR Hub) when IHttpContextAccessor is not set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user has access (200), false if 403 or 404.</returns>
    Task<bool> CanAccessBookingAsync(Guid bookingId, string? authorizationHeader = null, CancellationToken ct = default);
}
