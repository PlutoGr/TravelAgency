using TravelAgency.Booking.Domain.Exceptions;

namespace TravelAgency.Booking.Domain.Entities;

public class Favorite
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TourId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private Favorite() { }

    public static Favorite Create(Guid userId, Guid tourId)
    {
        if (userId == Guid.Empty)
            throw new BookingDomainException("userId must not be empty.");
        if (tourId == Guid.Empty)
            throw new BookingDomainException("tourId must not be empty.");

        return new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TourId = tourId,
            AddedAt = DateTime.UtcNow
        };
    }
}
