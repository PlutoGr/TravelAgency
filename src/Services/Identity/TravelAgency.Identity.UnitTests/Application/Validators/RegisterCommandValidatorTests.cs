using FluentAssertions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Auth.Commands.Register;

namespace TravelAgency.Identity.UnitTests.Application.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand ValidCommand() =>
        new(new RegisterRequest("user@example.com", "ValidPass1", "John", "Doe", null));

    [Fact]
    public async Task Validate_WithValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    public async Task Validate_WithInvalidEmail_FailsValidation(string email)
    {
        var command = new RegisterCommand(new RegisterRequest(email, "ValidPass1", "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Email"));
    }

    [Fact]
    public async Task Validate_EmailExceedingMaxLength_FailsValidation()
    {
        var longEmail = new string('a', 251) + "@b.com";
        var command = new RegisterCommand(new RegisterRequest(longEmail, "ValidPass1", "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithShortPassword_FailsValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "Ab1", "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Password"));
    }

    [Fact]
    public async Task Validate_WithPasswordMissingUppercase_FailsValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "password123", "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public async Task Validate_WithPasswordMissingLowercase_FailsValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "PASSWORD123", "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("lowercase"));
    }

    [Fact]
    public async Task Validate_WithPasswordMissingDigit_FailsValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "PasswordOnly", "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("digit"));
    }

    [Fact]
    public async Task Validate_WithPasswordExceedingMaxLength_FailsValidation()
    {
        var longPassword = "Aa1" + new string('x', 126);
        var command = new RegisterCommand(new RegisterRequest("u@e.com", longPassword, "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithEmptyFirstName_FailsValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "ValidPass1", "", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("FirstName"));
    }

    [Fact]
    public async Task Validate_WithFirstNameExceedingMaxLength_FailsValidation()
    {
        var longName = new string('a', 101);
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "ValidPass1", longName, "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithEmptyLastName_FailsValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "ValidPass1", "John", "", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("LastName"));
    }

    [Fact]
    public async Task Validate_WithNullPhone_PassesValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "ValidPass1", "John", "Doe", null));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidPhone_PassesValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "ValidPass1", "John", "Doe", "+1 (555) 123-4567"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithInvalidPhoneFormat_FailsValidation()
    {
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "ValidPass1", "John", "Doe", "not#a@phone!"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Phone"));
    }

    [Fact]
    public async Task Validate_WithPhoneTooLong_FailsValidation()
    {
        var longPhone = new string('1', 21);
        var command = new RegisterCommand(new RegisterRequest("u@e.com", "ValidPass1", "John", "Doe", longPhone));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
