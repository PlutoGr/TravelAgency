namespace TravelAgency.Identity.Application.Abstractions;

/// <summary>
/// Порт доставки писем аккаунта. Транспорт (SMTP, очередь) остаётся задаче отправки писем.
/// Метод возвращается сразу и не ждёт ящик. <see cref="AccountMail.Deliver"/> равный false
/// означает отказ от доставки: реализация не делает лишнего ввода-вывода.
/// </summary>
public interface IAccountMailSender
{
    Task<bool> SendAsync(AccountMail mail, CancellationToken cancellationToken = default);
}

public sealed record AccountMail(
    string Recipient,
    string Subject,
    string Text,
    string? LinkLabel,
    string? LinkUrl,
    bool Deliver)
{
    public static AccountMail Decline(string recipient) =>
        new(recipient, "", "", null, null, Deliver: false);
}
