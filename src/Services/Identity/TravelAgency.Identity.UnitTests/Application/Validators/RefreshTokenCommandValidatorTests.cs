using FluentAssertions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Auth.Commands.RefreshToken;

namespace TravelAgency.Identity.UnitTests.Application.Validators;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithNonEmptyToken_PassesValidation()
    {
        var command = new RefreshTokenCommand(new RefreshTokenRequest("someValidToken123"));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyToken_FailsValidation()
    {
        var command = new RefreshTokenCommand(new RefreshTokenRequest(""));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RefreshToken"));
    }
}
