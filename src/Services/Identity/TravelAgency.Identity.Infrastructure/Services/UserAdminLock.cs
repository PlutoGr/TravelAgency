using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.Infrastructure.Services;

public sealed class UserAdminLock(IdentityDbContext dbContext, ILogger<UserAdminLock> logger) : IUserAdminLock
{
    public const long PostgresKey = 170170;

    private const int Attempts = 2;

    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await ExecuteInTransactionAsync(action, cancellationToken);
                }
                catch (Exception exception) when (attempt < Attempts && IsSerializationConflict(exception))
                {
                    // Гонка двух снятий роли обрывает транзакцию ошибкой базы (SQLite busy
                    // или Postgres serialization_failure). Повтор после отката заново
                    // проверяет, что активный администратор остаётся.
                    logger.LogWarning(exception, "Повтор операции с пользователем после конфликта транзакции");
                    dbContext.ChangeTracker.Clear();
                }
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (dbContext.Database.IsNpgsql())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({PostgresKey})",
                cancellationToken);
        }

        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static bool IsSerializationConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var type = current.GetType();
            // Postgres: serialization_failure, deadlock_detected.
            var sqlState = type.GetProperty("SqlState")?.GetValue(current) as string;
            if (sqlState is "40001" or "40P01")
                return true;

            // SQLite: SQLITE_BUSY, SQLITE_LOCKED.
            if (type.GetProperty("SqliteErrorCode")?.GetValue(current) is int sqliteCode && sqliteCode is 5 or 6)
                return true;
        }

        return false;
    }
}
