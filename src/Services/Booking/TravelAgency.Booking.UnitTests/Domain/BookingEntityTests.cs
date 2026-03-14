using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Booking.Domain.ValueObjects;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Domain;

public class BookingEntityTests
{
    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid TourId = Guid.NewGuid();

    private static TourSnapshot CreateSnapshot() =>
        new TourSnapshot(
            Guid.NewGuid(), "Test Tour", "Description", 1000m, "USD", 7,
            DateTime.UtcNow);

    private static BookingEntity CreateNewBooking() =>
        BookingEntity.Create(ClientId, TourId, null);

    private static BookingEntity CreateInProgressBooking()
    {
        var booking = CreateNewBooking();
        booking.TransitionTo(BookingStatus.InProgress, ManagerId);
        return booking;
    }

    private static BookingEntity CreateProposalSentBooking()
    {
        var booking = CreateInProgressBooking();
        booking.AddProposal(ManagerId, CreateSnapshot(), null);
        booking.TransitionTo(BookingStatus.ProposalSent, ManagerId);
        return booking;
    }

    private static BookingEntity CreateConfirmedBooking()
    {
        var booking = CreateProposalSentBooking();
        var proposalId = booking.Proposals.First().Id;
        booking.ConfirmProposal(proposalId);
        return booking;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldCreateBookingWithNewStatus()
    {
        var before = DateTime.UtcNow;
        var booking = BookingEntity.Create(ClientId, TourId, "comment");
        var after = DateTime.UtcNow;

        booking.Id.Should().NotBe(Guid.Empty);
        booking.ClientId.Should().Be(ClientId);
        booking.TourId.Should().Be(TourId);
        booking.Comment.Should().Be("comment");
        booking.Status.Should().Be(BookingStatus.New);
        booking.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        booking.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyClientId_ShouldThrowBookingDomainException()
    {
        var act = () => BookingEntity.Create(Guid.Empty, TourId, null);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*ClientId*");
    }

    [Fact]
    public void Create_WithEmptyTourId_ShouldThrowBookingDomainException()
    {
        var act = () => BookingEntity.Create(ClientId, Guid.Empty, null);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*TourId*");
    }

    [Fact]
    public void Create_ShouldHaveInitialStatusHistoryEntry()
    {
        var booking = CreateNewBooking();

        booking.StatusHistory.Should().HaveCount(1);
        booking.StatusHistory.First().Status.Should().Be(BookingStatus.New);
    }

    // ── TransitionTo: valid transitions ───────────────────────────────────────

    [Fact]
    public void TransitionTo_NewToInProgress_ShouldSucceed()
    {
        var booking = CreateNewBooking();

        booking.TransitionTo(BookingStatus.InProgress, ManagerId);

        booking.Status.Should().Be(BookingStatus.InProgress);
        booking.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void TransitionTo_NewToCancelled_ShouldSucceed()
    {
        var booking = CreateNewBooking();

        booking.TransitionTo(BookingStatus.Cancelled, ClientId);

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void TransitionTo_InProgressToProposalSent_WithProposals_ShouldSucceed()
    {
        var booking = CreateInProgressBooking();
        booking.AddProposal(ManagerId, CreateSnapshot(), null);

        booking.TransitionTo(BookingStatus.ProposalSent, ManagerId);

        booking.Status.Should().Be(BookingStatus.ProposalSent);
    }

    [Fact]
    public void TransitionTo_InProgressToProposalSent_WithoutProposals_ShouldThrow()
    {
        var booking = CreateInProgressBooking();

        var act = () => booking.TransitionTo(BookingStatus.ProposalSent, ManagerId);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*proposal*");
    }

    [Fact]
    public void TransitionTo_InProgressToCancelled_ShouldSucceed()
    {
        var booking = CreateInProgressBooking();

        booking.TransitionTo(BookingStatus.Cancelled, ManagerId);

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void TransitionTo_ProposalSentToConfirmed_ShouldSucceed()
    {
        var booking = CreateProposalSentBooking();
        var proposalId = booking.Proposals.First().Id;

        booking.ConfirmProposal(proposalId);

        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void TransitionTo_ProposalSentToInProgress_ShouldSucceed()
    {
        var booking = CreateProposalSentBooking();

        booking.TransitionTo(BookingStatus.InProgress, ManagerId);

        booking.Status.Should().Be(BookingStatus.InProgress);
    }

    [Fact]
    public void TransitionTo_ProposalSentToCancelled_ShouldSucceed()
    {
        var booking = CreateProposalSentBooking();

        booking.TransitionTo(BookingStatus.Cancelled, ManagerId);

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void TransitionTo_ConfirmedToClosed_ShouldSucceed()
    {
        var booking = CreateConfirmedBooking();

        booking.TransitionTo(BookingStatus.Closed, ManagerId);

        booking.Status.Should().Be(BookingStatus.Closed);
    }

    [Fact]
    public void TransitionTo_ConfirmedToCancelled_ShouldSucceed()
    {
        var booking = CreateConfirmedBooking();

        booking.TransitionTo(BookingStatus.Cancelled, ManagerId);

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    // ── TransitionTo: invalid transitions ─────────────────────────────────────

    [Theory]
    [InlineData(BookingStatus.New)]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.ProposalSent)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    public void TransitionTo_ClosedToAny_ShouldThrow(BookingStatus target)
    {
        var booking = CreateConfirmedBooking();
        booking.TransitionTo(BookingStatus.Closed, ManagerId);

        var act = () => booking.TransitionTo(target, ManagerId);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*Cannot transition*");
    }

    [Theory]
    [InlineData(BookingStatus.New)]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.ProposalSent)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Closed)]
    public void TransitionTo_CancelledToAny_ShouldThrow(BookingStatus target)
    {
        var booking = CreateNewBooking();
        booking.TransitionTo(BookingStatus.Cancelled, ClientId);

        var act = () => booking.TransitionTo(target, ManagerId);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public void TransitionTo_NewToConfirmed_ShouldThrow()
    {
        var booking = CreateNewBooking();

        var act = () => booking.TransitionTo(BookingStatus.Confirmed, ManagerId);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*Cannot transition*");
    }

    // ── TransitionTo: status history ─────────────────────────────────────────

    [Fact]
    public void TransitionTo_ShouldAppendStatusHistoryEntry()
    {
        var booking = CreateNewBooking();

        booking.TransitionTo(BookingStatus.InProgress, ManagerId);

        booking.StatusHistory.Should().HaveCount(2);
        booking.StatusHistory.Last().Status.Should().Be(BookingStatus.InProgress);
    }

    // ── AddProposal ───────────────────────────────────────────────────────────

    [Fact]
    public void AddProposal_ShouldAddProposalAndOutboxMessage()
    {
        var booking = CreateInProgressBooking();

        booking.AddProposal(ManagerId, CreateSnapshot(), "some notes");

        booking.Proposals.Should().HaveCount(1);
        booking.OutboxMessages.Should().HaveCount(1);
        booking.OutboxMessages.First().EventType.Should().Be("ProposalSent");
    }

    [Fact]
    public void AddProposal_ToClosedBooking_ShouldThrow()
    {
        var booking = CreateConfirmedBooking();
        booking.TransitionTo(BookingStatus.Closed, ManagerId);

        var act = () => booking.AddProposal(ManagerId, CreateSnapshot(), null);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*Closed*");
    }

    [Fact]
    public void AddProposal_ToCancelledBooking_ShouldThrow()
    {
        var booking = CreateNewBooking();
        booking.TransitionTo(BookingStatus.Cancelled, ClientId);

        var act = () => booking.AddProposal(ManagerId, CreateSnapshot(), null);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*Cancelled*");
    }

    [Fact]
    public void AddProposal_WithEmptyManagerId_ShouldThrow()
    {
        var booking = CreateInProgressBooking();

        var act = () => booking.AddProposal(Guid.Empty, CreateSnapshot(), null);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*ManagerId*");
    }

    // ── ConfirmProposal ───────────────────────────────────────────────────────

    [Fact]
    public void ConfirmProposal_WithValidProposalId_ShouldConfirmAndTransitionToConfirmed()
    {
        var booking = CreateProposalSentBooking();
        var proposalId = booking.Proposals.First().Id;

        booking.ConfirmProposal(proposalId);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.Proposals.First().IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ConfirmProposal_WithInvalidProposalId_ShouldThrow()
    {
        var booking = CreateProposalSentBooking();

        var act = () => booking.ConfirmProposal(Guid.NewGuid());

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*not found*");
    }
}
