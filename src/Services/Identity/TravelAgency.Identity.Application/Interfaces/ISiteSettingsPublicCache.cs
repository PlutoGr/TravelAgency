using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Interfaces;

public interface ISiteSettingsPublicCache
{
    Task<SiteSettingsPublicDto?> TryGetAsync(CancellationToken cancellationToken = default);
    Task SetAsync(SiteSettingsPublicDto value, CancellationToken cancellationToken = default);
    Task RemoveAsync(CancellationToken cancellationToken = default);
}

public static class SiteSettingsCache
{
    public const string PublicKey = "site-settings:public";
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
}
