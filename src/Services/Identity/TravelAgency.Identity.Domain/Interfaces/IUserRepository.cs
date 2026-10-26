using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;

namespace TravelAgency.Identity.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Stage(User user);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);

    Task<(IReadOnlyList<User> Items, int Total)> SearchAsync(
        string? term,
        UserRole? role,
        bool? blocked,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> CountActiveAdminsAsync(CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);
}
