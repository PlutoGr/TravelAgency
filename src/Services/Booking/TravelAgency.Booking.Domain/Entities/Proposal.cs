using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Booking.Domain.ValueObjects;

namespace TravelAgency.Booking.Domain.Entities;

public class Proposal
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid ManagerId { get; private set; }
    public TourSnapshot TourSnapshot { get; private set; } = null!;
    public string? Notes { get; private set; }
    public bool IsConfirmed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Proposal() { }

    public static Proposal Create(Guid bookingId, Guid managerId, TourSnapshot snapshot, string? notes)
    {
        return new Proposal
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            ManagerId = managerId,
            TourSnapshot = snapshot,
            Notes = notes,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        if (IsConfirmed)
            throw new BookingDomainException("Proposal is already confirmed.");

        IsConfirmed = true;
    }
}
