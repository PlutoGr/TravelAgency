using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Application.Settings;

namespace TravelAgency.Media.UnitTests.Application.Validators;

public class UploadMediaCommandValidatorTests
{
    private readonly UploadMediaCommandValidator _validator;

    public UploadMediaCommandValidatorTests()
    {
        var settings = new UploadSettings
        {
            MaxFileSizeBytes = 10 * 1024 * 1024,
            AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"],
            ThumbnailWidths = [200, 800]
        };

        _validator = new UploadMediaCommandValidator(Options.Create(settings));
    }

    private static UploadMediaCommand ValidCommand() =>
        new(new MemoryStream([1, 2, 3]), "photo.jpg", "image/jpeg", 1024);

    [Fact]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyFileName_FailsWithRequiredMessage()
    {
        var command = ValidCommand() with { FileName = "" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("File name is required.");
    }

    [Fact]
    public async Task Validate_WhitespaceFileName_FailsValidation()
    {
        var command = ValidCommand() with { FileName = "   " };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    [InlineData("image/bmp")]
    public async Task Validate_DisallowedContentType_FailsValidation(string contentType)
    {
        var command = ValidCommand() with { ContentType = contentType };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    public async Task Validate_AllowedContentTypes_PassValidation(string contentType)
    {
        var command = ValidCommand() with { ContentType = contentType };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public async Task Validate_SizeBytesZero_FailsWithEmptyFileMessage()
    {
        var command = ValidCommand() with { SizeBytes = 0 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SizeBytes)
            .WithErrorMessage("File must not be empty.");
    }

    [Fact]
    public async Task Validate_NegativeSizeBytes_FailsValidation()
    {
        var command = ValidCommand() with { SizeBytes = -1 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SizeBytes);
    }

    [Fact]
    public async Task Validate_SizeBytesOverLimit_FailsValidation()
    {
        var command = ValidCommand() with { SizeBytes = 10 * 1024 * 1024 + 1 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SizeBytes);
    }

    [Fact]
    public async Task Validate_SizeBytesAtExactLimit_PassesValidation()
    {
        var command = ValidCommand() with { SizeBytes = 10 * 1024 * 1024 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.SizeBytes);
    }

    [Fact]
    public async Task Validate_EmptyContentType_FailsValidation()
    {
        var command = ValidCommand() with { ContentType = "" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }
}
