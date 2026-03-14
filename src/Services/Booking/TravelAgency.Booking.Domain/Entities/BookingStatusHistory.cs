using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Exceptions;

namespace TravelAgency.Booking.Domain.Entities;

public class BookingStatusHistory
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public BookingStatus Status { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private BookingStatusHistory() { }

    public static BookingStatusHistory Create(Guid bookingId, BookingStatus status, Guid changedByUserId)
    {
        if (bookingId == Guid.Empty)
            throw new BookingDomainException("bookingId must not be empty.");
        if (changedByUserId == Guid.Empty)
            throw new BookingDomainException("changedByUserId must not be empty.");

        return new BookingStatusHistory
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            Status = status,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow
        };
    }
}
