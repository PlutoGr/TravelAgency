using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Application.Abstractions;

public enum EmailChangeConsumption
{
    Changed,
    Taken,
    Lost
}

public interface IAccountLinkStore
{
    Task<PasswordResetToken?> FindPasswordResetAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task ReplacePasswordResetAsync(Guid userId, PasswordResetToken created, CancellationToken cancellationToken = default);

    /// <summary>
    /// Одно условное гашение: новый хеш, версия сессии и отзыв всех refresh в одной транзакции.
    /// false, если строку уже погасили параллельным запросом.
    /// </summary>
    Task<bool> TryConsumePasswordResetAsync(
        Guid tokenId,
        User user,
        string passwordHash,
        CancellationToken cancellationToken = default);

    Task<EmailChangeRequest?> FindEmailChangeByConfirmAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<EmailChangeRequest?> FindEmailChangeByCancelAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task ReplaceEmailChangeAsync(Guid userId, EmailChangeRequest? created, CancellationToken cancellationToken = default);

    Task<EmailChangeConsumption> TryConsumeEmailConfirmationAsync(
        Guid requestId,
        User user,
        string newEmail,
        Guid? keepRefreshId,
        CancellationToken cancellationToken = default);

    Task<bool> TryConsumeEmailCancellationAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task ApplySettingsPasswordAsync(
        User user,
        string passwordHash,
        Func<User, RefreshToken> stageRefresh,
        CancellationToken cancellationToken = default);
}
