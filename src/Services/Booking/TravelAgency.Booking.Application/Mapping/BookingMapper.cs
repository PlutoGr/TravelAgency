using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.ValueObjects;

namespace TravelAgency.Booking.Application.Mapping;

public static class BookingMapper
{
    public static BookingDto ToDto(this Domain.Entities.Booking booking) =>
        new(
            booking.Id,
            booking.ClientId,
            booking.TourId,
            booking.Comment,
            booking.Status,
            booking.CreatedAt,
            booking.UpdatedAt,
            booking.Proposals.Select(p => p.ToDto()).ToList().AsReadOnly());

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

    public static TourSnapshotDto ToDto(this TourSnapshot snapshot) =>
        new(
            snapshot.TourId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Price,
            snapshot.Currency,
            snapshot.DurationDays,
            snapshot.SnapshotTakenAt);
}
