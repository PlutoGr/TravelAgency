using Grpc.Core;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Commands.AssignManager;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class AssignManagerCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IOutboxMessageRepository> _outboxRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdentityGrpcClient> _identityGrpcMock = new();
    private readonly AssignManagerCommandHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid TargetManagerId = Guid.NewGuid();

    public AssignManagerCommandHandlerTests()
    {
        _handler = new AssignManagerCommandHandler(
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
            new AssignManagerCommand(Guid.NewGuid(), new AssignManagerRequest(TargetManagerId, null, null)),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ForbiddenException>();
        exception.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_WhenTargetIsNotManager_ShouldThrowBadRequest()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ActorId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Admin);
        _identityGrpcMock
            .Setup(c => c.GetUserRoleAsync(TargetManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleLookup(true, AppRoles.Client));

        var act = async () => await _handler.Handle(
            new AssignManagerCommand(Guid.NewGuid(), new AssignManagerRequest(TargetManagerId, null, null)),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Message.Should().Contain("не менеджер");
        _bookingRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetIsMissing_ShouldThrowBadRequest()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ActorId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Admin);
        _identityGrpcMock
            .Setup(c => c.GetUserRoleAsync(TargetManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleLookup(false, string.Empty));

        var act = async () => await _handler.Handle(
            new AssignManagerCommand(Guid.NewGuid(), new AssignManagerRequest(TargetManagerId, null, null)),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<BadRequestException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Message.Should().Contain("не менеджер");
        _bookingRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Internal)]
    public async Task Handle_WhenIdentityTransportFails_ShouldPropagateRpcException(StatusCode statusCode)
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ActorId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Admin);
        _identityGrpcMock
            .Setup(c => c.GetUserRoleAsync(TargetManagerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RpcException(new Status(statusCode, "identity down")));

        var act = async () => await _handler.Handle(
            new AssignManagerCommand(Guid.NewGuid(), new AssignManagerRequest(TargetManagerId, null, null)),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(statusCode);
        _bookingRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetIsManager_ShouldAssign()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ActorId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);
        _identityGrpcMock
            .Setup(c => c.GetUserRoleAsync(TargetManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleLookup(true, AppRoles.Manager));
        _identityGrpcMock
            .Setup(c => c.GetUserSummaryAsync(TargetManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSummary(TargetManagerId.ToString(), "manager@example.com", "Test", "Manager", AppRoles.Manager));
        _identityGrpcMock
            .Setup(c => c.GetUserSummaryAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummary?)null);

        var booking = BookingEntity.Create(ClientId, Guid.NewGuid(), null);
        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var result = await _handler.Handle(
            new AssignManagerCommand(booking.Id, new AssignManagerRequest(TargetManagerId, null, "передача")),
            CancellationToken.None);

        result.AssignedManagerId.Should().Be(TargetManagerId);
        result.AssignmentHistory.Should().ContainSingle();
        result.AssignmentHistory![0].Comment.Should().Be("передача");
        result.AssignedManagerName.Should().Be("Test Manager");
    }
}
