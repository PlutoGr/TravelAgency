namespace TravelAgency.Shared.Contracts.Auth;

/// <summary>
/// Общая фраза запроса восстановления. Её же отдаёт шлюз, если предел IP сработал раньше сервиса.
/// </summary>
public static class AccountLinkMessages
{
    public const string PasswordResetRequested =
        "Если такой адрес зарегистрирован, мы отправили на него письмо со ссылкой. Проверьте почту и папку «Спам»";

    public const string EmailChangeRequested =
        "Мы отправили письмо на новый адрес. Пока вы не подтвердите его, вход остаётся по прежнему адресу.";
}
