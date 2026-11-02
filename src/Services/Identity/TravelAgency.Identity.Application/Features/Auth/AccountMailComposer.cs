using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.Settings;

namespace TravelAgency.Identity.Application.Features.Auth;

public sealed class AccountMailComposer(IOptions<AccountLinkSettings> settings)
{
    public bool CanLink => !string.IsNullOrWhiteSpace(settings.Value.PublicBaseUrl);

    public AccountMail PasswordReset(string recipient, string secret)
    {
        var link = $"{BaseUrl()}/account/password/new?token={secret}";
        return new AccountMail(
            recipient,
            "Восстановление пароля",
            "Чтобы задать новый пароль, перейдите по ссылке. Она действует 1 час и только один раз. Если вы не запрашивали восстановление, ничего делать не нужно.",
            "Задать новый пароль",
            link,
            Deliver: true);
    }

    public AccountMail PasswordChanged(string recipient) =>
        new(
            recipient,
            "Пароль изменён",
            "Пароль вашего аккаунта изменён. Если это были не вы, восстановите доступ.",
            "Восстановить доступ",
            $"{BaseUrl()}/?auth=forgot",
            Deliver: true);

    public AccountMail ConfirmEmail(string recipient, string secret) =>
        new(
            recipient,
            "Подтвердите новый адрес",
            "Подтвердите этот адрес для входа. Ссылка действует 24 часа и только один раз. Если вы не запрашивали смену, ничего делать не нужно.",
            "Подтвердить адрес",
            $"{BaseUrl()}/account/email/confirm?token={secret}",
            Deliver: true);

    public AccountMail EmailChangeNotice(string recipient, string newEmail, string cancelSecret) =>
        new(
            recipient,
            "Запрошена смена адреса",
            $"Запрошена смена адреса входа на {newEmail}. Пока новый адрес не подтверждён, вход остаётся прежним. Если это были не вы, отмените запрос.",
            "Это был не я",
            $"{BaseUrl()}/account/email/cancel?token={cancelSecret}",
            Deliver: true);

    public AccountMail EmailTaken(string recipient) =>
        new(
            recipient,
            "Попытка привязать ваш адрес",
            "Кто-то пытался привязать этот адрес к другому аккаунту.",
            null,
            null,
            Deliver: true);

    private string BaseUrl() => settings.Value.PublicBaseUrl.Trim().TrimEnd('/');
}
