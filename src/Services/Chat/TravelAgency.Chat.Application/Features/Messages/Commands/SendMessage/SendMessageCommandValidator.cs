using FluentValidation;

namespace TravelAgency.Chat.Application.Features.Messages.Commands.SendMessage;

/// <summary>
/// Validates SendMessageCommand: Text or Attachments required, BookingId not empty, Text max length.
/// </summary>
public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    private const int MaxTextLength = 10_000;

    public SendMessageCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId must not be empty.");

        RuleFor(x => x)
            .Must(c => !string.IsNullOrWhiteSpace(c.Text) || (c.Attachments is { Count: > 0 }))
            .WithMessage("Text or Attachments must be provided.");

        RuleFor(x => x.Text)
            .MaximumLength(MaxTextLength)
            .When(x => x.Text is not null)
            .WithMessage($"Text must not exceed {MaxTextLength} characters.");
    }
}
