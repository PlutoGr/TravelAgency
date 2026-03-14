using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IBookingRepository> _bookingRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateBookingCommandHandler _handler;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid TourId = Guid.NewGuid();

    public CreateBookingCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(ClientId);
        _currentUserMock.Setup(u => u.Role).Returns("Client");

        _handler = new CreateBookingCommandHandler(
            _currentUserMock.Object,
            _bookingRepoMock.Object,
            _unitOfWorkMock.Object);
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
    public async Task Handle_WhenUnitOfWorkThrows_ShouldPropagateException()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        var command = new CreateBookingCommand(new CreateBookingRequest(TourId, null));
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB failure");
    }
}
