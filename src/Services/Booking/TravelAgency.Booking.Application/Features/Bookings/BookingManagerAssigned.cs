using System.Text.Json;
using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Application.Features.Bookings;

internal static class BookingManagerAssigned
{
    public const string EventType = "BookingManagerAssigned";

    public static OutboxMessage CreateMessage(
        Guid bookingId,
        Guid? previousManagerId,
        Guid newManagerId,
        Guid changedBy)
    {
        var payload = JsonSerializer.Serialize(new
        {
            BookingId = bookingId,
            PreviousManagerId = previousManagerId,
            NewManagerId = newManagerId,
            ChangedBy = changedBy
        });

        return OutboxMessage.Create(EventType, payload);
    }
}
