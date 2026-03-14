using FluentValidation;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Settings;

namespace TravelAgency.Media.Application.Features.Upload;

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator(IOptions<UploadSettings> settings)
    {
        var s = settings.Value;

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => s.AllowedMimeTypes.Contains(ct))
            .WithMessage($"Content type must be one of: {string.Join(", ", s.AllowedMimeTypes)}.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("File must not be empty.")
            .LessThanOrEqualTo(s.MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {s.MaxFileSizeBytes / (1024 * 1024)} MB.");
    }
}
