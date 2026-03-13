using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TravelAgency.Gateway.HealthChecks;

public class DownstreamServiceHealthCheck(
    IHttpClientFactory httpClientFactory, string serviceUrl) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("HealthChecks");
            client.Timeout = Timeout;
            var response = await client.GetAsync(serviceUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{context.Registration.Name} is reachable")
                : HealthCheckResult.Degraded($"{context.Registration.Name} returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                $"{context.Registration.Name} is unreachable: {ex.Message}");
        }
    }
}
