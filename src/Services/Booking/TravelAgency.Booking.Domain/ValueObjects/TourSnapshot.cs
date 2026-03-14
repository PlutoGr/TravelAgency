namespace TravelAgency.Booking.Domain.ValueObjects;

public record TourSnapshot(
    Guid TourId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int DurationDays,
    DateTime SnapshotTakenAt);
