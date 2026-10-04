using FluentValidation;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Services;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain;

namespace TravelAgency.Media.Application.Features.Upload;

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator(IOptions<UploadSettings> settings)
    {
        var s = settings.Value;

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.FileContent)
            .Must(stream => stream != null && stream.CanSeek)
            .WithMessage("File stream must be seekable for validation.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => s.AllowedMimeTypes.Contains(ct))
            .WithMessage($"Content type must be one of: {string.Join(", ", s.AllowedMimeTypes)}.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(s.MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {s.MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.Purpose)
            .Must(purpose => string.IsNullOrWhiteSpace(purpose) || MediaPurposes.IsTourImage(purpose))
            .WithMessage("Purpose must be 'tour-image' when provided.");

        When(x => MediaPurposes.IsTourImage(x.Purpose), () =>
        {
            RuleFor(x => x.ContentType)
                .Must(TourImageContentTypes.IsAllowed)
                .WithMessage("Tour images must be jpeg, png, or webp.");
        });

        RuleFor(x => x)
            .MustAsync(ValidateContentMatchesDeclaredType)
            .WithMessage("File content does not match declared content type.");
    }

    private static async Task<bool> ValidateContentMatchesDeclaredType(UploadMediaCommand command, CancellationToken ct)
    {
        return await FileContentValidator.ValidateAsync(command.FileContent, command.ContentType, ct);
    }
}
