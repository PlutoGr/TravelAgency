using FluentAssertions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Auth.Commands.Login;

namespace TravelAgency.Identity.UnitTests.Application.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_PassesValidation()
    {
        var command = new LoginCommand(new LoginRequest("user@example.com", "password123"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    [InlineData("noatsign.com")]
    public async Task Validate_WithInvalidEmail_FailsValidation(string email)
    {
        var command = new LoginCommand(new LoginRequest(email, "password"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Email"));
    }

    [Fact]
    public async Task Validate_WithEmptyPassword_FailsValidation()
    {
        var command = new LoginCommand(new LoginRequest("user@example.com", ""));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Password"));
    }

    [Fact]
    public async Task Validate_WithEmptyEmailAndPassword_HasTwoErrors()
    {
        var command = new LoginCommand(new LoginRequest("", ""));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
