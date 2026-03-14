using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Queries.GetBookingById;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Queries;

public class GetBookingByIdQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly GetBookingByIdQueryHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();

    public GetBookingByIdQueryHandlerTests()
    {
        _handler = new GetBookingByIdQueryHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object);
    }

    private BookingEntity CreateBookingForClient(Guid clientId) =>
        BookingEntity.Create(clientId, Guid.NewGuid(), null);

    [Fact]
    public async Task Handle_ExistingBooking_ClientIsOwner_ShouldReturnBooking()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateBookingForClient(ClientId);
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var result = await _handler.Handle(new GetBookingByIdQuery(bookingId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(bookingId);
        result.ClientId.Should().Be(ClientId);
    }

    [Fact]
    public async Task Handle_ExistingBooking_ManagerRole_ShouldReturnBooking()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var booking = CreateBookingForClient(ClientId);
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var result = await _handler.Handle(new GetBookingByIdQuery(bookingId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(bookingId);
    }

    [Fact]
    public async Task Handle_ExistingBooking_ClientIsNotOwner_ShouldThrowForbiddenException()
    {
        var differentClientId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.UserId).Returns(differentClientId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Client);

        var booking = CreateBookingForClient(ClientId);
        var bookingId = booking.Id;

        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var act = async () => await _handler.Handle(new GetBookingByIdQuery(bookingId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*own*");
    }

    [Fact]
    public async Task Handle_BookingNotFound_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ManagerId);
        _currentUserMock.Setup(u => u.Role).Returns(AppRoles.Manager);

        var bookingId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity?)null);

        var act = async () => await _handler.Handle(new GetBookingByIdQuery(bookingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
