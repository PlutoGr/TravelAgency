using System.Text.Json;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Booking.Domain.ValueObjects;

namespace TravelAgency.Booking.Domain.Entities;

public class Booking
{
    private readonly List<Proposal> _proposals = [];
    private readonly List<BookingStatusHistory> _statusHistory = [];
    private readonly List<OutboxMessage> _outboxMessages = [];

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid TourId { get; private set; }
    public string? Comment { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<Proposal> Proposals => _proposals.AsReadOnly();
    public IReadOnlyCollection<BookingStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<OutboxMessage> OutboxMessages => _outboxMessages.AsReadOnly();

    private Booking() { }

    public static Booking Create(Guid clientId, Guid tourId, string? comment)
    {
        if (clientId == Guid.Empty)
            throw new BookingDomainException("ClientId must not be empty.");

        if (tourId == Guid.Empty)
            throw new BookingDomainException("TourId must not be empty.");

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            TourId = tourId,
            Comment = comment,
            Status = BookingStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        booking._statusHistory.Add(
            BookingStatusHistory.Create(booking.Id, BookingStatus.New, clientId));

        return booking;
    }

    public void TransitionTo(BookingStatus newStatus, Guid changedByUserId)
    {
        if (!IsValidTransition(Status, newStatus))
            throw new BookingDomainException(
                $"Cannot transition booking from '{Status}' to '{newStatus}'.");

        if (newStatus == BookingStatus.ProposalSent && _proposals.Count == 0)
            throw new BookingDomainException(
                "At least one proposal must exist before transitioning to 'ProposalSent'.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        _statusHistory.Add(BookingStatusHistory.Create(Id, newStatus, changedByUserId));
    }

    public void AddProposal(Guid managerId, TourSnapshot snapshot, string? notes)
    {
        if (managerId == Guid.Empty)
            throw new BookingDomainException("ManagerId must not be empty.");

        if (Status is BookingStatus.Closed or BookingStatus.Cancelled)
            throw new BookingDomainException(
                $"Cannot add a proposal to a booking with status '{Status}'.");

        var proposal = Proposal.Create(Id, managerId, snapshot, notes);
        _proposals.Add(proposal);

        var payload = JsonSerializer.Serialize(new
        {
            BookingId = Id,
            ProposalId = proposal.Id,
            ManagerId = managerId,
            TourId = snapshot.TourId,
            snapshot.Title,
            snapshot.Price,
            snapshot.Currency
        });

        _outboxMessages.Add(OutboxMessage.Create("ProposalSent", payload));

        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmProposal(Guid proposalId)
    {
        var proposal = _proposals.FirstOrDefault(p => p.Id == proposalId)
            ?? throw new BookingDomainException($"Proposal '{proposalId}' not found in this booking.");

        proposal.Confirm();
        TransitionTo(BookingStatus.Confirmed, ClientId);
    }

    private static bool IsValidTransition(BookingStatus current, BookingStatus next) =>
        (current, next) switch
        {
            (BookingStatus.New, BookingStatus.InProgress) => true,
            (BookingStatus.New, BookingStatus.Cancelled) => true,
            (BookingStatus.InProgress, BookingStatus.ProposalSent) => true,
            (BookingStatus.InProgress, BookingStatus.Cancelled) => true,
            (BookingStatus.ProposalSent, BookingStatus.Confirmed) => true,
            (BookingStatus.ProposalSent, BookingStatus.InProgress) => true,
            (BookingStatus.ProposalSent, BookingStatus.Cancelled) => true,
            (BookingStatus.Confirmed, BookingStatus.Closed) => true,
            (BookingStatus.Confirmed, BookingStatus.Cancelled) => true,
            _ => false
        };
}
