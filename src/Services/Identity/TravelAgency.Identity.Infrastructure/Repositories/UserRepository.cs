using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Domain.Entities;
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
}
