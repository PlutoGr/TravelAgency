using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<ICatalogGrpcClient> _catalogGrpcMock = new();
    private readonly Mock<IOutboxMessageRepository> _outboxRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdentityGrpcClient> _identityGrpcMock = new();
    private readonly CreateBookingCommandHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid TourId = Guid.NewGuid();

    public CreateBookingCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns("Client");

        _catalogGrpcMock
            .Setup(c => c.GetTourSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingTourSnapshotDto(TourId, "Tour", "Desc", 100m, "USD", 7, DateTime.UtcNow));

        _handler = new CreateBookingCommandHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object,
            _catalogGrpcMock.Object,
            _outboxRepoMock.Object,
            _unitOfWorkMock.Object,
            _identityGrpcMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateAndStageBooking()
    {
        var command = new CreateBookingCommand(new CreateBookingRequest(TourId, "comment"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.TourId.Should().Be(TourId);
        result.ClientId.Should().Be(ClientId);

        _bookingRepoMock.Verify(r => r.Stage(It.IsAny<BookingEntity>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnBookingDtoWithNewStatus()
    {
        var command = new CreateBookingCommand(new CreateBookingRequest(TourId, null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(BookingStatus.New);
    }

    [Fact]
    public async Task Handle_WhenTourNotFound_ShouldPropagateNotFoundException()
    {
        _catalogGrpcMock
            .Setup(c => c.GetTourSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Tour not found"));

        var command = new CreateBookingCommand(new CreateBookingRequest(TourId, null));
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkThrows_ShouldPropagateException()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        var command = new CreateBookingCommand(new CreateBookingRequest(TourId, null));
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB failure");
    }

    [Fact]
    public async Task Handle_WhenClientHasPinnedManager_AssignsThatManagerAndWritesHistory()
    {
        var managerId = Guid.NewGuid();
        _bookingRepoMock
            .Setup(r => r.GetPreferredManagerIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerId);
        _identityGrpcMock
            .Setup(c => c.GetUserSummaryAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSummary(managerId.ToString(), "manager@example.com", "Test", "Manager", "Manager"));

        BookingEntity? staged = null;
        _bookingRepoMock
            .Setup(r => r.Stage(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(booking => staged = booking);

        var result = await _handler.Handle(
            new CreateBookingCommand(new CreateBookingRequest(TourId, null)),
            CancellationToken.None);

        staged.Should().NotBeNull();
        staged!.AssignedManagerId.Should().Be(managerId);
        staged.AssignmentHistory.Should().ContainSingle();
        staged.AssignmentHistory.Single().NewManagerId.Should().Be(managerId);
        staged.AssignmentHistory.Single().PreviousManagerId.Should().BeNull();
        result.AssignedManagerId.Should().Be(managerId);
        result.AssignedManagerName.Should().Be("Test Manager");
        result.AssignmentHistory.Should().BeNull();

        _outboxRepoMock.Verify(
            r => r.Stage(It.Is<OutboxMessage>(m => m.EventType == "BookingManagerAssigned")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientHasNoPinnedManager_LeavesBookingInQueue()
    {
        _bookingRepoMock
            .Setup(r => r.GetPreferredManagerIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        BookingEntity? staged = null;
        _bookingRepoMock
            .Setup(r => r.Stage(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(booking => staged = booking);

        var result = await _handler.Handle(
            new CreateBookingCommand(new CreateBookingRequest(TourId, null)),
            CancellationToken.None);

        staged.Should().NotBeNull();
        staged!.AssignedManagerId.Should().BeNull();
        staged.AssignedAt.Should().BeNull();
        staged.AssignmentHistory.Should().BeEmpty();
        result.AssignedManagerId.Should().BeNull();

        _outboxRepoMock.Verify(
            r => r.Stage(It.Is<OutboxMessage>(m => m.EventType == "BookingManagerAssigned")),
            Times.Never);
    }
}
