using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TravelAgency.Gateway.HealthChecks;

public class DownstreamServiceHealthCheck(
    IHttpClientFactory httpClientFactory, string serviceUrl, ILogger<DownstreamServiceHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("HealthChecks");
            var response = await client.GetAsync(serviceUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{context.Registration.Name} is reachable")
                : HealthCheckResult.Degraded($"{context.Registration.Name} returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check failed for {ServiceName}", context.Registration.Name);
            return HealthCheckResult.Degraded(
                description: $"{context.Registration.Name} is unreachable",
                exception: ex);
        }
    }
}
