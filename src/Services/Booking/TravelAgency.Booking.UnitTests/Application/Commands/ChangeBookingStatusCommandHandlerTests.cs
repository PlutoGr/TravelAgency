using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Commands.ChangeBookingStatus;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class ChangeBookingStatusCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ChangeBookingStatusCommandHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();

    public ChangeBookingStatusCommandHandlerTests()
    {
        _handler = new ChangeBookingStatusCommandHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private BookingEntity CreateNewBooking(Guid? clientId = null) =>
        BookingEntity.Create(clientId ?? ClientId, Guid.NewGuid(), null);

    [Fact]
    public async Task Handle_Manager_CanTransitionNewToInProgress()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var booking = CreateNewBooking();
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ChangeBookingStatusCommand(
            bookingId,
            new ChangeBookingStatusRequest(BookingStatus.InProgress));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(BookingStatus.InProgress);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Client_CanCancelOwnBooking()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateNewBooking(ClientId);
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ChangeBookingStatusCommand(
            bookingId,
            new ChangeBookingStatusRequest(BookingStatus.Cancelled));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_Client_CannotDoManagerTransition()
    {
        var differentClientId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(differentClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateNewBooking(ClientId);
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ChangeBookingStatusCommand(
            bookingId,
            new ChangeBookingStatusRequest(BookingStatus.InProgress));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_Client_CannotTransitionToNonCancelledStatus()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateNewBooking(ClientId);
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ChangeBookingStatusCommand(
            bookingId,
            new ChangeBookingStatusRequest(BookingStatus.InProgress));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*cancel*");
    }

    [Fact]
    public async Task Handle_BookingNotFound_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var bookingId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity?)null);

        var command = new ChangeBookingStatusCommand(
            bookingId,
            new ChangeBookingStatusRequest(BookingStatus.InProgress));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
