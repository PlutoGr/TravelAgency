namespace TravelAgency.Identity.Application.Features.Auth;

/// <summary>
/// Правило пароля регистрации: 8…128 символов, заглавная, строчная и цифра. Один код на любое нарушение.
/// </summary>
public static class PasswordRules
{
    public const string Code = "PasswordRule";
    public const string MismatchCode = "PasswordMismatch";

    public static bool IsSatisfied(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 128)
            return false;

        var upper = false;
        var lower = false;
        var digit = false;
        foreach (var character in password)
        {
            if (character is >= 'A' and <= 'Z')
                upper = true;
            else if (character is >= 'a' and <= 'z')
                lower = true;
            else if (character is >= '0' and <= '9')
                digit = true;
        }

        return upper && lower && digit;
    }
}
