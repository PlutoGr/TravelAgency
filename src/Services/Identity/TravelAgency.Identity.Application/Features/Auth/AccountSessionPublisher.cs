using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.Application.Features.Auth;

public sealed class AccountSessionPublisher(
    ISessionRevocationStore store,
    IOptions<JwtSettings> jwtSettings,
    ILogger<AccountSessionPublisher> logger)
{
    private const int Attempts = 3;
    private static readonly TimeSpan RetryBase = TimeSpan.FromMilliseconds(50);

    public async Task PublishAsync(Guid userId, long version, bool blocked, CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromMinutes(Math.Max(1, jwtSettings.Value.AccessTokenExpirationMinutes) + 2);
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                await store.SetAsync(userId, version, blocked, ttl, cancellationToken);
                await store.PublishAsync(userId, cancellationToken);
                return;
            }
            catch (Exception exception)
            {
                if (cancellationToken.IsCancellationRequested || attempt == Attempts)
                {
                    logger.LogWarning(exception, "Не удалось записать отзыв сессии {UserId}", userId);
                    return;
                }

                await Task.Delay(RetryBase * attempt, CancellationToken.None);
            }
        }
    }
}
