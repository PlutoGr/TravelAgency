using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Domain.Interfaces;

public interface ISiteSettingsRepository
{
    Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SiteSettings settings, CancellationToken cancellationToken = default);
    Task AddAuditAsync(IReadOnlyList<SiteSettingsAudit> entries, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteSettingsAudit>> GetRecentAuditAsync(int take, CancellationToken cancellationToken = default);
}
