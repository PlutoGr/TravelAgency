using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Application.Features.Admin;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.Infrastructure.Repositories;

public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);
    }

    public void Stage(User user) => dbContext.Users.Add(user);

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        // Entity is expected to be tracked (e.g. from GetByIdAsync). Change tracking persists modifications; Update() is redundant.
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
    }

    public async Task<(IReadOnlyList<User> Items, int Total)> SearchAsync(
        string? term,
        UserRole? role,
        bool? blocked,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var trimmed = term.Trim();
            var pattern = UserSearch.LikePattern(trimmed);
            if (dbContext.Database.IsNpgsql())
            {
                query = query.Where(user =>
                    EF.Functions.ILike(user.Email, pattern, "\\")
                    || EF.Functions.ILike(user.FirstName, pattern, "\\")
                    || EF.Functions.ILike(user.LastName, pattern, "\\")
                    || (user.Phone != null && EF.Functions.ILike(user.Phone, pattern, "\\")));
            }
            else
            {
                // SQLite lower() не меняет кириллицу, поэтому рядом с свёрнутым шаблоном
                // ищется исходный регистр запроса.
                var typed = UserSearch.LikePattern(trimmed, foldCase: false);
                query = query.Where(user =>
                    EF.Functions.Like(user.Email, pattern, "\\")
                    || EF.Functions.Like(user.Email, typed, "\\")
                    || EF.Functions.Like(user.FirstName, pattern, "\\")
                    || EF.Functions.Like(user.FirstName, typed, "\\")
                    || EF.Functions.Like(user.LastName, pattern, "\\")
                    || EF.Functions.Like(user.LastName, typed, "\\")
                    || (user.Phone != null && (
                        EF.Functions.Like(user.Phone, pattern, "\\")
                        || EF.Functions.Like(user.Phone, typed, "\\"))));
            }
        }

        if (role is not null)
            query = query.Where(user => user.Role == role);

        if (blocked is true)
            query = query.Where(user => user.BlockedAt != null);
        else if (blocked is false)
            query = query.Where(user => user.BlockedAt == null);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken ct = default) =>
        dbContext.Users.CountAsync(user => user.Role == UserRole.Admin && user.BlockedAt == null, ct);

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var rows = await dbContext.Users.AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.FirstName, user.LastName })
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.Id, row => $"{row.FirstName} {row.LastName}".Trim());
    }
}
