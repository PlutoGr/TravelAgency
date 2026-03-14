namespace TravelAgency.Booking.Application.DTOs;

public record ProposalDto(
    Guid Id,
    Guid BookingId,
    Guid ManagerId,
    TourSnapshotDto TourSnapshot,
    string? Notes,
    bool IsConfirmed,
    DateTime CreatedAt);
