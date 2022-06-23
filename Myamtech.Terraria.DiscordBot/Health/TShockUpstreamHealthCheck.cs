using JetBrains.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Myamtech.Terraria.DiscordBot.Health;

[UsedImplicitly]
public class TShockUpstreamHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}