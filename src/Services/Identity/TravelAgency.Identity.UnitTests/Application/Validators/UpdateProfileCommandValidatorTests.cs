using FluentValidation.TestHelper;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

namespace TravelAgency.Identity.UnitTests.Application.Validators;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyRequest_IsValid()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidNamesAndE164Phone_IsValid()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest("John", "Doe", "+1234567890"));
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_FirstNameTooLong_HasError()
    {
        var longName = new string('a', 101);
        var command = new UpdateProfileCommand(new UpdateProfileRequest(longName, null, null));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("firstName");
    }

    [Fact]
    public void Validate_LastNameTooLong_HasError()
    {
        var longName = new string('a', 101);
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, longName, null));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("lastName");
    }

    [Fact]
    public void Validate_InvalidPhone_HasError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, "not#a@phone!"));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("phone");
    }

    [Fact]
    public void Validate_PhoneWithSpaces_HasError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, "+44 20 7946 0958"));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("phone");
    }

    [Fact]
    public void Validate_EmptyPhone_IsValid()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(null, null, ""));
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor("phone");
    }

    [Fact]
    public void Validate_EmptyFirstName_HasError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest("", null, null));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("firstName");
    }

    [Fact]
    public void Validate_TourismSinceInFuture_HasError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(TourismSince: DateTime.UtcNow.Year + 1));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("tourismSince")
            .WithErrorMessage("Укажите год не позже текущего.");
    }

    [Fact]
    public void Validate_UnknownLanguage_HasError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(Languages: ["xx"]));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("languages");
    }

    [Fact]
    public void Validate_ManagerFields_AreValid()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(
            About: "Опыт",
            TourismSince: 2010,
            Languages: ["ru", "en"],
            Specialization: "Европа"));
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CityTooLong_HasError()
    {
        var command = new UpdateProfileCommand(new UpdateProfileRequest(City: new string('c', 101)));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("city");
    }
}
