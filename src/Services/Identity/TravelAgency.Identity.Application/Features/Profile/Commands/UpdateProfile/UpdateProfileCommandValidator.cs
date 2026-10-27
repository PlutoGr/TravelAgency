using FluentValidation;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Application.Features.Profile;

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

        RuleFor(x => x.Request.City)
            .Must(city => city is null || city.Trim().Length <= User.MaxCityLength)
            .WithMessage("Город не длиннее 100 символов.")
            .OverridePropertyName("city");

        RuleFor(x => x.Request.About)
            .Must(about => about is null || about.Length <= User.MaxAboutLength)
            .WithMessage("Текст «О себе» не длиннее 1000 символов.")
            .OverridePropertyName("about");

        RuleFor(x => x.Request.TourismSince)
            .Must(year => year is null || (year >= User.MinTourismSinceYear && year <= DateTime.UtcNow.Year))
            .WithMessage("Укажите год не позже текущего.")
            .OverridePropertyName("tourismSince");

        RuleFor(x => x.Request.Specialization)
            .Must(value => value is null || value.Length <= User.MaxSpecializationLength)
            .WithMessage("Специализация не длиннее 300 символов.")
            .OverridePropertyName("specialization");

        RuleFor(x => x.Request.Languages)
            .Custom((languages, context) =>
            {
                if (languages is null)
                    return;

                for (var i = 0; i < languages.Count; i++)
                {
                    if (!AllowedLanguages.IsAllowed(languages[i]))
                    {
                        context.AddFailure(
                            "languages",
                            $"Язык «{languages[i]}» не из списка ISO 639-1.");
                    }
                }
            });
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
            && System.Text.RegularExpressions.Regex.IsMatch(trimmed, PhonePattern);
    }
}
