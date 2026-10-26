using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(IdentityDbContext dbContext) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var hash = RefreshToken.Hash(token);
        return await dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hash, ct);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);
    }

    public void Stage(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        // Entity is expected to be tracked (e.g. from GetByTokenAsync). Change tracking persists modifications; Update() is redundant.
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), ct);

        foreach (var local in dbContext.RefreshTokens.Local
                     .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
                     .ToList())
        {
            dbContext.Entry(local).State = EntityState.Detached;
        }
    }

    public async Task<bool> TryRotateAsync(
        Guid activeTokenId,
        RefreshToken replacement,
        string replacementPlaintext,
        CancellationToken ct = default)
    {
        var tracked = dbContext.RefreshTokens.Local.FirstOrDefault(token => token.Id == activeTokenId);
        if (tracked is not null)
            dbContext.Entry(tracked).State = EntityState.Detached;

        var now = DateTime.UtcNow;
        var replacementHash = RefreshToken.Hash(replacementPlaintext);

        // Условный UPDATE: второй параллельный запрос меняет 0 строк и не получает новый токен.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var affected = await dbContext.RefreshTokens
            .Where(token => token.Id == activeTokenId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.ReplacedByToken, replacementHash),
                ct);

        if (affected != 1)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        dbContext.RefreshTokens.Add(replacement);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}
