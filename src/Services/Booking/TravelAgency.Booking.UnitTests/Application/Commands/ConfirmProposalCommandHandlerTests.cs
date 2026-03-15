using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Commands.ConfirmProposal;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Domain.ValueObjects;
using TravelAgency.Shared.Contracts.Authorization;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class ConfirmProposalCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ConfirmProposalCommandHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();

    public ConfirmProposalCommandHandlerTests()
    {
        _handler = new ConfirmProposalCommandHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private BookingEntity CreateBookingWithProposal(Guid? clientId = null)
    {
        var booking = BookingEntity.Create(clientId ?? ClientId, Guid.NewGuid(), null);
        booking.TransitionTo(BookingStatus.InProgress, ManagerId);
        booking.AddProposal(ManagerId, CreateTourSnapshot(), "notes");
        booking.TransitionTo(BookingStatus.ProposalSent, ManagerId);
        return booking;
    }

    private static TourSnapshot CreateTourSnapshot() =>
        new(Guid.NewGuid(), "Tour", "Desc", 100m, "USD", 5, DateTime.UtcNow);

    [Fact]
    public async Task Handle_ClientConfirmsOwnBooking_ShouldSucceed()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateBookingWithProposal(ClientId);
        var proposalId = booking.Proposals.First().Id;
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmProposalCommand(bookingId, new ConfirmProposalRequest(proposalId));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.Proposals.Should().ContainSingle(p => p.Id == proposalId && p.IsConfirmed);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ManagerConfirmsAnyBooking_ShouldSucceed()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var booking = CreateBookingWithProposal(ClientId);
        var proposalId = booking.Proposals.First().Id;
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmProposalCommand(bookingId, new ConfirmProposalRequest(proposalId));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Confirmed);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var bookingId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity?)null);

        var command = new ConfirmProposalCommand(bookingId, new ConfirmProposalRequest(Guid.NewGuid()));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Booking '{bookingId}'*");
    }

    [Fact]
    public async Task Handle_ProposalNotFound_ShouldThrowBookingDomainException()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateBookingWithProposal(ClientId);
        var nonExistentProposalId = Guid.NewGuid();
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmProposalCommand(bookingId, new ConfirmProposalRequest(nonExistentProposalId));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BookingDomainException>()
            .WithMessage($"*Proposal '{nonExistentProposalId}'*");
    }

    [Fact]
    public async Task Handle_ClientTriesToConfirmOtherClientsBooking_ShouldThrowForbiddenException()
    {
        var otherClientId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(otherClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateBookingWithProposal(ClientId);
        var proposalId = booking.Proposals.First().Id;
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmProposalCommand(bookingId, new ConfirmProposalRequest(proposalId));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*own bookings*");
    }
}
