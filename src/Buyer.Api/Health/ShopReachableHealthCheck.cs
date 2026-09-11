using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Buyer.Api.Health;

public sealed class ShopReachableHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("shop");
            using var response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("shop reachable")
                : HealthCheckResult.Unhealthy($"shop health HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("shop unreachable", ex);
        }
    }
}
