namespace TravelAgency.Booking.Application.DTOs;

public record ProposalDto(
    Guid Id,
    Guid BookingId,
    Guid ManagerId,
    BookingTourSnapshotDto TourSnapshot,
    string? Notes,
    bool IsConfirmed,
    DateTime CreatedAt);
