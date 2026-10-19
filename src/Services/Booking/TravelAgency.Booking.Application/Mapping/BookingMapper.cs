using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.ValueObjects;

namespace TravelAgency.Booking.Application.Mapping;

public static class BookingMapper
{
    public static BookingDto ToDto(
        this Domain.Entities.Booking booking,
        string? clientName = null,
        string? clientEmail = null,
        string? clientPhone = null,
        string? assignedManagerName = null,
        bool includeAssignmentHistory = false) =>
        new(
            booking.Id,
            booking.ClientId,
            booking.TourId,
            booking.Comment,
            booking.Status,
            booking.CreatedAt,
            booking.UpdatedAt,
            booking.Proposals.Select(p => p.ToDto()).ToList().AsReadOnly(),
            clientName,
            clientEmail,
            clientPhone,
            booking.AssignedManagerId,
            booking.AssignedAt,
            assignedManagerName,
            includeAssignmentHistory
                ? booking.AssignmentHistory
                    .OrderBy(h => h.ChangedAt)
                    .ThenBy(h => h.Id)
                    .Select(h => new BookingAssignmentHistoryDto(
                        h.Id,
                        h.PreviousManagerId,
                        h.NewManagerId,
                        h.ChangedByUserId,
                        h.ChangedAt,
                        h.Comment))
                    .ToList()
                : null);

    public static ProposalDto ToDto(this Proposal proposal) =>
        new(
            proposal.Id,
            proposal.BookingId,
            proposal.ManagerId,
            proposal.TourSnapshot.ToDto(),
            proposal.Notes,
            proposal.IsConfirmed,
            proposal.CreatedAt);

    public static FavoriteDto ToDto(this Favorite favorite) =>
        new(
            favorite.Id,
            favorite.UserId,
            favorite.TourId,
            favorite.AddedAt);

    public static BookingTourSnapshotDto ToDto(this TourSnapshot snapshot) =>
        new BookingTourSnapshotDto(
            snapshot.TourId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Price,
            snapshot.Currency,
            snapshot.DurationDays,
            snapshot.SnapshotTakenAt);
}
