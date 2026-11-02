using System.Text.RegularExpressions;
using FluentValidation;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public const string PhonePattern = @"^\+[1-9]\d{1,14}$";

    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Request.FirstName)
            .Must(name => name is null || IsName(name))
            .WithMessage("Укажите имя длиной от 1 до 100 символов.")
            .OverridePropertyName("firstName");

        RuleFor(x => x.Request.LastName)
            .Must(name => name is null || IsName(name))
            .WithMessage("Укажите фамилию длиной от 1 до 100 символов.")
            .OverridePropertyName("lastName");

        RuleFor(x => x.Request.Phone)
            .Must(phone => phone is null || phone.Length == 0 || IsPhone(phone))
            .WithMessage("Телефон должен быть в формате E.164, например +79991234567, или пустым.")
            .OverridePropertyName("phone");
    }

    private static bool IsName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.Length is >= 1 and <= 100;
    }

    private static bool IsPhone(string phone)
    {
        var trimmed = phone.Trim();
        return trimmed.Length <= 20
            && Regex.IsMatch(trimmed, PhonePattern);
    }
}
