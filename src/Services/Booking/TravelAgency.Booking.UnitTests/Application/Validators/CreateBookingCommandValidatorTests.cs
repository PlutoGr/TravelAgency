using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;

namespace TravelAgency.Booking.UnitTests.Application.Validators;

public class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidTourId_ShouldBeValid()
    {
        var command = new CreateBookingCommand(new CreateBookingRequest(Guid.NewGuid(), null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyTourId_ShouldBeInvalid()
    {
        var command = new CreateBookingCommand(new CreateBookingRequest(Guid.Empty, null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("TourId"));
    }

    [Fact]
    public async Task Validate_WithComment_ShouldBeValid()
    {
        var command = new CreateBookingCommand(new CreateBookingRequest(Guid.NewGuid(), "I want a window seat"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullComment_ShouldBeValid()
    {
        var command = new CreateBookingCommand(new CreateBookingRequest(Guid.NewGuid(), null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
