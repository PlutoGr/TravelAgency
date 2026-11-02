namespace TravelAgency.Identity.Application.Settings;

public sealed class AccountLinkSettings
{
    public const string SectionName = "AccountLinks";

    public TimeSpan PasswordResetLifetime { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan EmailChangeLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Абсолютный origin без хвоста. Пустая строка значит, что письмо со ссылкой отправить нельзя.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "";
}
