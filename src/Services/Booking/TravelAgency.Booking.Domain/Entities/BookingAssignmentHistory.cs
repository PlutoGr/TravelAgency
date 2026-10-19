using TravelAgency.Booking.Domain.Exceptions;

namespace TravelAgency.Booking.Domain.Entities;

public class BookingAssignmentHistory
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid? PreviousManagerId { get; private set; }
    public Guid NewManagerId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? Comment { get; private set; }

    private BookingAssignmentHistory() { }

    public static BookingAssignmentHistory Create(
        Guid bookingId,
        Guid? previousManagerId,
        Guid newManagerId,
        Guid changedByUserId,
        string? comment)
    {
        if (bookingId == Guid.Empty)
            throw new BookingDomainException("bookingId must not be empty.");
        if (newManagerId == Guid.Empty)
            throw new BookingDomainException("newManagerId must not be empty.");
        if (changedByUserId == Guid.Empty)
            throw new BookingDomainException("changedByUserId must not be empty.");
        if (comment is { Length: > 500 })
            throw new BookingDomainException("Comment must not exceed 500 characters.");

        return new BookingAssignmentHistory
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            PreviousManagerId = previousManagerId,
            NewManagerId = newManagerId,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Comment = comment
        };
    }
}
