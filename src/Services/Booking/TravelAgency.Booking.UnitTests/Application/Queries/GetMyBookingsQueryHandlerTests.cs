using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.Features.Bookings.Queries.GetMyBookings;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Queries;

public class GetMyBookingsQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly GetMyBookingsQueryHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();

    public GetMyBookingsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);

        _handler = new GetMyBookingsQueryHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCurrentUserBookings()
    {
        var booking1 = BookingEntity.Create(ClientId, Guid.NewGuid(), null);
        var booking2 = BookingEntity.Create(ClientId, Guid.NewGuid(), "comment");

        _bookingRepoMock.Setup(r => r.GetByClientIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity> { booking1, booking2 }.AsReadOnly());

        var result = await _handler.Handle(new GetMyBookingsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.ClientId.Should().Be(ClientId));
        result.Should().AllSatisfy(b => b.Status.Should().Be(BookingStatus.New));
    }

    [Fact]
    public async Task Handle_WhenNoBookings_ShouldReturnEmptyList()
    {
        _bookingRepoMock.Setup(r => r.GetByClientIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity>().AsReadOnly());

        var result = await _handler.Handle(new GetMyBookingsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldQueryRepositoryWithCurrentUserId()
    {
        _bookingRepoMock.Setup(r => r.GetByClientIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity>().AsReadOnly());

        await _handler.Handle(new GetMyBookingsQuery(), CancellationToken.None);

        _bookingRepoMock.Verify(r => r.GetByClientIdAsync(ClientId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
