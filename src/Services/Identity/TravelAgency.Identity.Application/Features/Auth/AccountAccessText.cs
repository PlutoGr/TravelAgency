namespace TravelAgency.Identity.Application.Features.Auth;

public static class AccountAccessText
{
    public const string BlockedPrefix = "Доступ к аккаунту ограничен. Свяжитесь с поддержкой";

    public static string Blocked(string? supportPhone)
    {
        if (string.IsNullOrWhiteSpace(supportPhone))
            return BlockedPrefix + ".";

        return BlockedPrefix + ": " + supportPhone.Trim();
    }
}
