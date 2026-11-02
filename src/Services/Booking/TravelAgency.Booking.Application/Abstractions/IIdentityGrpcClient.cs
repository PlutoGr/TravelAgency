namespace TravelAgency.Booking.Application.Abstractions;

public record UserSummary(string UserId, string Email, string FirstName, string LastName, string Role);

public readonly record struct UserRoleLookup(bool UserFound, string Role);

public enum IdentityReadKind
{
    NotFound = 0,
    Found = 1,
    Unavailable = 2
}

/// <summary>
/// Сводка для карточки клиента. Телефона нет: номер читается отдельным вызовом.
/// </summary>
public sealed record IdentityCardProfile(
    string Email,
    string DisplayName,
    string Role,
    string? AvatarUrl);

public readonly record struct IdentityCardProfileRead(IdentityReadKind Kind, IdentityCardProfile? Profile);

public readonly record struct UserPhoneRead(IdentityReadKind Kind, string Phone);

public interface IIdentityGrpcClient
{
    Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken ct = default);

    Task<UserRoleLookup> GetUserRoleAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Чтение сводки, которое отличает NotFound от прочего сбоя. RpcException не гасится в null.
    /// </summary>
    Task<IdentityCardProfileRead> ReadCardProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Номер учётной записи. Пустая строка при Found значит, что номера нет. NotFound — это не пустой номер.
    /// </summary>
    Task<UserPhoneRead> ReadUserPhoneAsync(Guid userId, CancellationToken ct = default);
}
