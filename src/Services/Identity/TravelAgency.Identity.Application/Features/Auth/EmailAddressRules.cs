using FluentValidation;

namespace TravelAgency.Identity.Application.Features.Auth;

/// <summary>
/// Адрес как при регистрации: не пустой после обрезки, EmailAddress, не длиннее 256.
/// Сравнение и запись идут в нижнем регистре.
/// </summary>
public static class EmailAddressRules
{
    public const string InvalidCode = "InvalidEmail";
    public const string UnchangedCode = "EmailUnchanged";
    public const int MaxLength = 256;

    private static readonly EmailShapeValidator Shape = new();

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var trimmed = email.Trim();
        return trimmed.Length <= MaxLength && Shape.Validate(trimmed).IsValid;
    }

    private sealed class EmailShapeValidator : AbstractValidator<string>
    {
        public EmailShapeValidator()
        {
            RuleFor(value => value).EmailAddress();
        }
    }
}
