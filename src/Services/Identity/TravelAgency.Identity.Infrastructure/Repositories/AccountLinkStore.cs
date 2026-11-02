using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.Infrastructure.Repositories;

public sealed class AccountLinkStore(IdentityDbContext db) : IAccountLinkStore
{
    public Task<PasswordResetToken?> FindPasswordResetAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.PasswordResetTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task ReplacePasswordResetAsync(Guid userId, PasswordResetToken created, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.PasswordResetTokens
            .Where(token => token.UserId == userId && token.ConsumedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ConsumedAt, now), cancellationToken);
        DetachResets(userId);
        db.PasswordResetTokens.Add(created);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(created).State = EntityState.Detached;
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public async Task<bool> TryConsumePasswordResetAsync(
        Guid tokenId,
        User user,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        DetachReset(tokenId);
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.PasswordResetTokens
            .Where(token => token.Id == tokenId && token.ConsumedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ConsumedAt, now), cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        user.ChangePassword(passwordHash);
        user.RevokeSessions();
        await RevokeRefreshAsync(user.Id, keepId: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<EmailChangeRequest?> FindEmailChangeByConfirmAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.EmailChangeRequests.FirstOrDefaultAsync(request => request.ConfirmTokenHash == tokenHash, cancellationToken);

    public Task<EmailChangeRequest?> FindEmailChangeByCancelAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.EmailChangeRequests.FirstOrDefaultAsync(request => request.CancelTokenHash == tokenHash, cancellationToken);

    public async Task ReplaceEmailChangeAsync(Guid userId, EmailChangeRequest? created, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.EmailChangeRequests
            .Where(request => request.UserId == userId && request.ConsumedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(request => request.ConsumedAt, now), cancellationToken);
        DetachEmailChanges(userId);
        if (created is not null)
            db.EmailChangeRequests.Add(created);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (created is not null)
                db.Entry(created).State = EntityState.Detached;
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public async Task<EmailChangeConsumption> TryConsumeEmailConfirmationAsync(
        Guid requestId,
        User user,
        string newEmail,
        Guid? keepRefreshId,
        CancellationToken cancellationToken = default)
    {
        DetachEmailChange(requestId);
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.EmailChangeRequests
            .Where(request => request.Id == requestId && request.ConsumedAt == null && request.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(request => request.ConsumedAt, now), cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return EmailChangeConsumption.Lost;
        }

        var taken = await db.Users.AnyAsync(
            candidate => candidate.Email == newEmail && candidate.Id != user.Id,
            cancellationToken);
        if (taken)
        {
            await transaction.CommitAsync(cancellationToken);
            return EmailChangeConsumption.Taken;
        }

        await db.Database.ExecuteSqlRawAsync("SAVEPOINT email_change", cancellationToken);
        try
        {
            user.ChangeEmail(newEmail);
            await RevokeRefreshAsync(user.Id, keepRefreshId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync("RELEASE SAVEPOINT email_change", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return EmailChangeConsumption.Changed;
        }
        catch (DbUpdateException)
        {
            await db.Database.ExecuteSqlRawAsync("ROLLBACK TO SAVEPOINT email_change", cancellationToken);
            db.Entry(user).State = EntityState.Unchanged;
            await transaction.CommitAsync(cancellationToken);
            return EmailChangeConsumption.Taken;
        }
    }

    public async Task<bool> TryConsumeEmailCancellationAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        DetachEmailChange(requestId);
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.EmailChangeRequests
            .Where(request => request.Id == requestId && request.ConsumedAt == null && request.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(request => request.ConsumedAt, now), cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task ApplySettingsPasswordAsync(
        User user,
        string passwordHash,
        Func<User, RefreshToken> stageRefresh,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        user.ChangePassword(passwordHash);
        user.RevokeSessions();
        await RevokeRefreshAsync(user.Id, keepId: null, cancellationToken);
        db.RefreshTokens.Add(stageRefresh(user));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RevokeRefreshAsync(Guid userId, Guid? keepId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = db.RefreshTokens.Where(token =>
            token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now);
        if (keepId is Guid id)
            query = query.Where(token => token.Id != id);

        await query.ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
        foreach (var local in db.RefreshTokens.Local
                     .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now && token.Id != keepId)
                     .ToList())
        {
            db.Entry(local).State = EntityState.Detached;
        }
    }

    private void DetachResets(Guid userId)
    {
        foreach (var local in db.PasswordResetTokens.Local.Where(token => token.UserId == userId).ToList())
            db.Entry(local).State = EntityState.Detached;
    }

    private void DetachReset(Guid tokenId)
    {
        var tracked = db.PasswordResetTokens.Local.FirstOrDefault(token => token.Id == tokenId);
        if (tracked is not null)
            db.Entry(tracked).State = EntityState.Detached;
    }

    private void DetachEmailChanges(Guid userId)
    {
        foreach (var local in db.EmailChangeRequests.Local.Where(request => request.UserId == userId).ToList())
            db.Entry(local).State = EntityState.Detached;
    }

    private void DetachEmailChange(Guid requestId)
    {
        var tracked = db.EmailChangeRequests.Local.FirstOrDefault(request => request.Id == requestId);
        if (tracked is not null)
            db.Entry(tracked).State = EntityState.Detached;
    }
}
