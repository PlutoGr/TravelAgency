using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Commands.CreateProposal;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class CreateProposalCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<ICatalogGrpcClient> _catalogGrpcMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateProposalCommandHandler _handler;

    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid ClientId = Guid.NewGuid();

    public CreateProposalCommandHandlerTests()
    {
        _handler = new CreateProposalCommandHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object,
            _catalogGrpcMock.Object,
            _unitOfWorkMock.Object);
    }

    private BookingEntity CreateInProgressBooking()
    {
        var booking = BookingEntity.Create(ClientId, Guid.NewGuid(), null);
        booking.TransitionTo(BookingStatus.InProgress, ManagerId);
        return booking;
    }

    private TourSnapshotDto CreateSnapshotDto(Guid tourId) =>
        new TourSnapshotDto(tourId, "Test Tour", "Description", 2000m, "USD", 7, DateTime.UtcNow);

    [Fact]
    public async Task Handle_Manager_CanCreateProposal()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var booking = CreateInProgressBooking();
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _catalogGrpcMock.Setup(c => c.GetTourSnapshotAsync(booking.TourId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSnapshotDto(booking.TourId));

        var command = new CreateProposalCommand(bookingId, new CreateProposalRequest("optional notes"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ManagerId.Should().Be(ManagerId);
        result.Notes.Should().Be("optional notes");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonManager_ShouldThrowForbiddenException()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var command = new CreateProposalCommand(Guid.NewGuid(), new CreateProposalRequest(null));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*managers*");
    }

    [Fact]
    public async Task Handle_BookingNotFound_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var bookingId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity?)null);

        var command = new CreateProposalCommand(bookingId, new CreateProposalRequest(null));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldCallCatalogGrpcWithBookingTourId()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var booking = CreateInProgressBooking();
        var bookingId = booking.Id;
        var expectedTourId = booking.TourId;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _catalogGrpcMock.Setup(c => c.GetTourSnapshotAsync(expectedTourId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSnapshotDto(expectedTourId));

        var command = new CreateProposalCommand(bookingId, new CreateProposalRequest(null));

        await _handler.Handle(command, CancellationToken.None);

        _catalogGrpcMock.Verify(c => c.GetTourSnapshotAsync(expectedTourId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
