using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Interfaces;

namespace TravelAgency.Identity.Infrastructure.Caching;

public sealed class MemorySiteSettingsCache(IMemoryCache cache, ILogger<MemorySiteSettingsCache> logger) : ISiteSettingsPublicCache
{
    public Task<SiteSettingsPublicDto?> TryGetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cache.TryGetValue(SiteSettingsCache.PublicKey, out SiteSettingsPublicDto? value);
            return Task.FromResult(value);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Public site settings cache read failed");
            return Task.FromResult<SiteSettingsPublicDto?>(null);
        }
    }

    public Task SetAsync(SiteSettingsPublicDto value, CancellationToken cancellationToken = default)
    {
        try
        {
            cache.Set(SiteSettingsCache.PublicKey, value, SiteSettingsCache.Ttl);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Public site settings cache write failed");
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cache.Remove(SiteSettingsCache.PublicKey);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Public site settings cache removal failed");
        }

        return Task.CompletedTask;
    }
}
