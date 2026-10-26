using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Interfaces;

namespace TravelAgency.Identity.Infrastructure.Caching;

public sealed class DistributedSiteSettingsCache(
    IDistributedCache cache,
    ILogger<DistributedSiteSettingsCache> logger) : ISiteSettingsPublicCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SiteSettingsPublicDto?> TryGetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await cache.GetAsync(SiteSettingsCache.PublicKey, cancellationToken);
            if (bytes is null || bytes.Length == 0)
                return null;

            return JsonSerializer.Deserialize<SiteSettingsPublicDto>(bytes, JsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Public site settings cache read failed");
            return null;
        }
    }

    public async Task SetAsync(SiteSettingsPublicDto value, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await cache.SetAsync(
                SiteSettingsCache.PublicKey,
                bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SiteSettingsCache.Ttl },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Public site settings cache write failed");
        }
    }

    public async Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(SiteSettingsCache.PublicKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Public site settings cache removal failed");
        }
    }
}
