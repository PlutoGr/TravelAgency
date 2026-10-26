using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.Infrastructure.Repositories;

public sealed class SiteSettingsRepository(IdentityDbContext dbContext) : ISiteSettingsRepository
{
    public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        dbContext.SiteSettings.FirstOrDefaultAsync(x => x.Id == SiteSettings.SingletonId, cancellationToken);

    public async Task AddAsync(SiteSettings settings, CancellationToken cancellationToken = default) =>
        await dbContext.SiteSettings.AddAsync(settings, cancellationToken);

    public async Task AddAuditAsync(IReadOnlyList<SiteSettingsAudit> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
            return;

        await dbContext.SiteSettingsAudits.AddRangeAsync(entries, cancellationToken);
    }

    public async Task<IReadOnlyList<SiteSettingsAudit>> GetRecentAuditAsync(int take, CancellationToken cancellationToken = default)
    {
        // SQLite в интеграционных тестах не умеет ORDER BY по DateTimeOffset.
        // В PostgreSQL сортировка и лимит остаются в запросе.
        if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            var rows = await dbContext.SiteSettingsAudits.ToListAsync(cancellationToken);
            return rows
                .OrderByDescending(x => x.ChangedAt)
                .ThenByDescending(x => x.Id)
                .Take(take)
                .ToList();
        }

        return await dbContext.SiteSettingsAudits
            .OrderByDescending(x => x.ChangedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
