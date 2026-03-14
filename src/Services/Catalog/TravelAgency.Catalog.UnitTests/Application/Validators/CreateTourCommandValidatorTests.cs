using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Commands.CreateTour;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Validators;

public class CreateTourCommandValidatorTests
{
    private readonly CreateTourCommandValidator _validator = new();

    private static CreateTourCommand BuildCommand(
        string title = "Valid Tour Title",
        string description = "Valid description for the tour.",
        string country = "Greece",
        int durationDays = 7)
    {
        var request = new CreateTourRequest(title, description, TourType.Beach, country, durationDays, null, null);
        return new CreateTourCommand(request);
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        var command = BuildCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldHaveError()
    {
        var command = BuildCommand(title: "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Title"));
    }

    [Fact]
    public void Validate_WithEmptyCountry_ShouldHaveError()
    {
        var command = BuildCommand(country: "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Country"));
    }

    [Fact]
    public void Validate_WithZeroDurationDays_ShouldHaveError()
    {
        var command = BuildCommand(durationDays: 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("DurationDays"));
    }

    [Fact]
    public void Validate_WithTitleTooLong_ShouldHaveError()
    {
        var longTitle = new string('A', 201);
        var command = BuildCommand(title: longTitle);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Title"));
    }
}
