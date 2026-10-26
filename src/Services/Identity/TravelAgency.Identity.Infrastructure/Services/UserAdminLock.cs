using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.Infrastructure.Services;

public sealed class UserAdminLock(IdentityDbContext dbContext) : IUserAdminLock
{
    public const long PostgresKey = 170170;

    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
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
        finally
        {
            Gate.Release();
        }
    }
}
