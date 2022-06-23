using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Myamtech.Terraria.DiscordBot.Health;

[UsedImplicitly]
public class DiscordHealthCheck : IHealthCheck
{
    private readonly DiscordSocketClient _discord;

    public DiscordHealthCheck(
        DiscordSocketClient discordSocketClient
    )
    {
        _discord = discordSocketClient;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        return _discord.ConnectionState == ConnectionState.Connected ? 
            Task.FromResult(HealthCheckResult.Healthy()) :
            Task.FromResult(HealthCheckResult.Unhealthy("Discord socket was not connected"));
    }
}