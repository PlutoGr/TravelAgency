using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Commands.TakeBooking;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class TakeBookingCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IOutboxMessageRepository> _outboxRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdentityGrpcClient> _identityGrpcMock = new();
    private readonly TakeBookingCommandHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();

    public TakeBookingCommandHandlerTests()
    {
        _handler = new TakeBookingCommandHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object,
            _outboxRepoMock.Object,
            _unitOfWorkMock.Object,
            _identityGrpcMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClient_ShouldThrowForbiddenException()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var act = async () => await _handler.Handle(
            new TakeBookingCommand(Guid.NewGuid(), new TakeBookingRequest(null)),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ForbiddenException>();
        exception.Which.StatusCode.Should().Be(403);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenManagerTakesQueue_ShouldAssignAndStageEvent()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);
        _identityGrpcMock
            .Setup(c => c.GetUserSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSummary(ManagerId.ToString(), "manager@example.com", "Test", "Manager", AppRoles.Manager));

        var booking = BookingEntity.Create(ClientId, Guid.NewGuid(), null);
        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var result = await _handler.Handle(
            new TakeBookingCommand(booking.Id, new TakeBookingRequest(null)),
            CancellationToken.None);

        result.AssignedManagerId.Should().Be(ManagerId);
        result.AssignmentHistory.Should().ContainSingle();
        booking.AssignmentHistory.Should().ContainSingle();
        _outboxRepoMock.Verify(
            r => r.Stage(It.Is<OutboxMessage>(m => m.EventType == "BookingManagerAssigned")),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExpectedManagerDoesNotMatch_ShouldThrowConflict()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var booking = BookingEntity.Create(ClientId, Guid.NewGuid(), null);
        booking.AssignManager(Guid.NewGuid(), Guid.NewGuid(), null, null);
        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var act = async () => await _handler.Handle(
            new TakeBookingCommand(booking.Id, new TakeBookingRequest(null)),
            CancellationToken.None);

        await act.Should().ThrowAsync<BookingAssignmentConflictException>();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
