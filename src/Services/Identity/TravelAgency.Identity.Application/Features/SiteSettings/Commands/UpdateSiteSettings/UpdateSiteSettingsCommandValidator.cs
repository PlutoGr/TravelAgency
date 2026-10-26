using FluentValidation;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Commands.UpdateSiteSettings;

public sealed class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    public const string PhonePattern = @"^\+[1-9]\d{1,14}$";
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";

    public UpdateSiteSettingsCommandValidator()
    {
        RuleFor(x => x.Request.SiteName)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 120)
            .WithMessage("Укажите название сайта длиной от 1 до 120 символов.")
            .OverridePropertyName("siteName");

        RuleFor(x => x.Request.SupportPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || IsPhone(phone))
            .WithMessage("Телефон должен быть в формате E.164, например +79991234567.")
            .OverridePropertyName("supportPhone");

        RuleFor(x => x.Request.ContactEmail)
            .Must(email => string.IsNullOrWhiteSpace(email) || IsEmail(email))
            .WithMessage("Укажите email в формате user@example.com.")
            .OverridePropertyName("contactEmail");

        RuleFor(x => x.Request.OfficeAddress)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 300)
            .WithMessage("Адрес офиса не длиннее 300 символов.")
            .OverridePropertyName("officeAddress");

        RuleFor(x => x.Request.WorkingHours)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 200)
            .WithMessage("Часы работы не длиннее 200 символов.")
            .OverridePropertyName("workingHours");

        RuleFor(x => x.Request.BannerText)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 500)
            .WithMessage("Текст баннера не длиннее 500 символов.")
            .OverridePropertyName("bannerText");

        RuleFor(x => x.Request.SocialLinks)
            .Custom((links, context) =>
            {
                if (links is null)
                    return;

                if (links.Count > 10)
                    context.AddFailure("socialLinks", "Можно указать не больше 10 ссылок.");

                for (var index = 0; index < links.Count; index++)
                {
                    var label = links[index].Label?.Trim() ?? string.Empty;
                    var url = links[index].Url?.Trim() ?? string.Empty;

                    if (label.Length is < 1 or > 40)
                    {
                        context.AddFailure(
                            $"socialLinks[{index}].label",
                            "Подпись ссылки: от 1 до 40 символов.");
                    }

                    if (!IsHttpsUrl(url))
                    {
                        context.AddFailure(
                            $"socialLinks[{index}].url",
                            "Ссылка должна начинаться с https:// и быть не длиннее 500 символов.");
                    }
                }
            });
    }

    private static bool IsPhone(string phone)
    {
        var trimmed = phone.Trim();
        return trimmed.Length <= 20 && System.Text.RegularExpressions.Regex.IsMatch(trimmed, PhonePattern);
    }

    private static bool IsEmail(string email)
    {
        var trimmed = email.Trim();
        return trimmed.Length <= 254 && System.Text.RegularExpressions.Regex.IsMatch(trimmed, EmailPattern);
    }

    internal static bool IsHttpsUrl(string url) =>
        url.Length is > 8 and <= 500 && url.StartsWith("https://", StringComparison.Ordinal);
}
