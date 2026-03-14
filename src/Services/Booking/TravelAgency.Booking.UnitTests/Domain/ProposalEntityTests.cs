using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Booking.Domain.ValueObjects;

namespace TravelAgency.Booking.UnitTests.Domain;

public class ProposalEntityTests
{
    private static TourSnapshot CreateSnapshot() =>
        new TourSnapshot(
            Guid.NewGuid(), "Test Tour", "Description", 1500m, "EUR", 10,
            DateTime.UtcNow);

    [Fact]
    public void Create_WithValidData_ShouldCreateProposal()
    {
        var bookingId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var snapshot = CreateSnapshot();
        var before = DateTime.UtcNow;

        var proposal = Proposal.Create(bookingId, managerId, snapshot, "notes");
        var after = DateTime.UtcNow;

        proposal.Id.Should().NotBe(Guid.Empty);
        proposal.BookingId.Should().Be(bookingId);
        proposal.ManagerId.Should().Be(managerId);
        proposal.TourSnapshot.Should().Be(snapshot);
        proposal.Notes.Should().Be("notes");
        proposal.IsConfirmed.Should().BeFalse();
        proposal.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Confirm_ShouldSetIsConfirmedTrue()
    {
        var proposal = Proposal.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSnapshot(), null);

        proposal.Confirm();

        proposal.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrow()
    {
        var proposal = Proposal.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSnapshot(), null);
        proposal.Confirm();

        var act = () => proposal.Confirm();

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*already confirmed*");
    }
}
