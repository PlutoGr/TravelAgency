using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Mapping;

internal static class BookingView
{
    public static async Task<BookingDto> ForViewerAsync(
        Domain.Entities.Booking booking,
        string? role,
        IIdentityGrpcClient identityGrpcClient,
        CancellationToken cancellationToken)
    {
        var staff = role is AppRoles.Manager or AppRoles.Admin;
        string? clientName = null;
        string? clientEmail = null;

        if (staff)
        {
            var client = await identityGrpcClient.GetUserSummaryAsync(booking.ClientId, cancellationToken);
            if (client != null)
            {
                clientName = FormatName(client);
                clientEmail = client.Email;
            }
        }

        var managerName = await ResolveManagerNameAsync(
            booking.AssignedManagerId, identityGrpcClient, cancellationToken);

        return booking.ToDto(clientName, clientEmail, null, managerName, staff);
    }

    public static async Task<IReadOnlyList<BookingDto>> ForStaffListAsync(
        IReadOnlyList<Domain.Entities.Booking> bookings,
        IIdentityGrpcClient identityGrpcClient,
        CancellationToken cancellationToken)
    {
        var result = new List<BookingDto>(bookings.Count);
        var cache = new Dictionary<Guid, UserSummary?>();

        foreach (var booking in bookings)
        {
            var client = await GetSummaryAsync(cache, identityGrpcClient, booking.ClientId, cancellationToken);
            var clientName = client != null ? FormatName(client) : null;
            var managerName = booking.AssignedManagerId is Guid managerId
                ? FormatNameOrNull(await GetSummaryAsync(cache, identityGrpcClient, managerId, cancellationToken))
                : null;

            result.Add(booking.ToDto(clientName, client?.Email, null, managerName, includeAssignmentHistory: true));
        }

        return result.AsReadOnly();
    }

    public static async Task<string?> ResolveManagerNameAsync(
        Guid? managerId,
        IIdentityGrpcClient identityGrpcClient,
        CancellationToken cancellationToken)
    {
        if (managerId is not Guid id)
            return null;

        var summary = await identityGrpcClient.GetUserSummaryAsync(id, cancellationToken);
        return FormatNameOrNull(summary);
    }

    private static async Task<UserSummary?> GetSummaryAsync(
        Dictionary<Guid, UserSummary?> cache,
        IIdentityGrpcClient identityGrpcClient,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(userId, out var summary))
        {
            summary = await identityGrpcClient.GetUserSummaryAsync(userId, cancellationToken);
            cache[userId] = summary;
        }

        return summary;
    }

    private static string? FormatNameOrNull(UserSummary? summary) =>
        summary == null ? null : FormatName(summary);

    private static string FormatName(UserSummary summary) =>
        $"{summary.FirstName} {summary.LastName}".Trim();
}
