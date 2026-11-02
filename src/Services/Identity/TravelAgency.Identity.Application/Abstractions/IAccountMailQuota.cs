namespace TravelAgency.Identity.Application.Abstractions;

public enum AccountMailSlot
{
    Reserved,
    Exhausted,
    StoreUnavailable
}

/// <summary>
/// Не больше 3 писем на нормализованный адрес за скользящий час.
/// То же хранилище, что у счётчика входа: Redis, если он задан, иначе память процесса.
/// </summary>
public interface IAccountMailQuota
{
    public const string PasswordReset = "password-reset";
    public const string EmailChange = "email-change";

    Task<AccountMailSlot> TryReserveAsync(string bucket, string normalizedEmail, CancellationToken cancellationToken = default);

    Task ReleaseAsync(string bucket, string normalizedEmail, CancellationToken cancellationToken = default);
}
