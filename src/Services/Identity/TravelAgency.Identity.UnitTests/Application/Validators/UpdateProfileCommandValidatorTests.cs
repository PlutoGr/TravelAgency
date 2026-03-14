using FluentAssertions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

namespace TravelAgency.Identity.UnitTests.Application.Validators;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithAllNullFields_PassesValidation()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidFields_PassesValidation()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest("John", "Doe", "+1234567890"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithFirstNameExceedingMaxLength_FailsValidation()
    {
        var longName = new string('a', 101);
        var command = new UpdateProfileCommand(new UpdateProfileRequest(longName, null, null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("FirstName"));
    }

    [Fact]
    public async Task Validate_WithLastNameExceedingMaxLength_FailsValidation()
    {
        var longName = new string('a', 101);
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, longName, null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("LastName"));
    }

    [Fact]
    public async Task Validate_WithInvalidPhoneFormat_FailsValidation()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, "not#a@phone!"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Phone"));
    }

    [Fact]
    public async Task Validate_WithPhoneTooLong_FailsValidation()
    {
        var longPhone = new string('1', 21);
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, longPhone));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithEmptyPhone_PassesValidation()
    {
        // Empty string is treated as null (not present), so phone validation is skipped
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, ""));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidPhone_PassesValidation()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, "+44 20 7946 0958"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // FIX-012: .NotEmpty() added before .MaximumLength(100) — empty string must be rejected when field is provided

    [Fact]
    public async Task Validate_WhenFirstNameIsEmptyString_ReturnsValidationError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest("", null, null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("FirstName"));
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsNull_PassesValidation()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsValid_PassesValidation()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest("Alice", null, null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenLastNameIsEmptyString_ReturnsValidationError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, "", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("LastName"));
    }
}
