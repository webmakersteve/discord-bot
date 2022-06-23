using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Myamtech.Terraria.DiscordBot.Configuration;
using Myamtech.Terraria.DiscordBot.Database;
using Myamtech.Terraria.DiscordBot.Terraria;
using Serilog;
using Serilog.Context;
using ILogger = Serilog.ILogger;

namespace Myamtech.Terraria.DiscordBot.Services;

public class TShockScraper : BackgroundService
{
    private const long GuildId = 259438654890573839;
    private static readonly ILogger Logger = Log.Logger.ForContext<TShockScraper>();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TerrariaServerCache _cache;
    private readonly DiscordSocketClient _discordClient;
    private readonly TerrariaEmbedRepository _embedRepository;

    public TShockScraper(
        DiscordSocketClient discordClient,
        TerrariaServerCache cache,
        TerrariaEmbedRepository embedRepository,
        IServiceScopeFactory scopeFactory
    )
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _embedRepository = embedRepository;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var terrariaConfigs = scope.ServiceProvider
                .GetRequiredService<IOptionsSnapshot<TerrariaTargetsConfiguration>>();
            var thisRunScrapeTargets = terrariaConfigs.Value.Targets;

            if (thisRunScrapeTargets.Count == 0)
            {
                Logger.Information("No current configured scrape targets...");
            }
            
            foreach (var target in thisRunScrapeTargets)
            {
                await RunScrapeForTargetAsync(scope, target, stoppingToken);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task RunScrapeForTargetAsync(
        IServiceScope serviceScope,
        TerrariaTargetsConfiguration.TerrariaTarget target, 
        CancellationToken stoppingToken
    )
    {
        try
        {
            using var _ = LogContext.PushProperty("TerrariaTarget", target.Name);
            var tShockClient = serviceScope.ServiceProvider.GetRequiredService<TShockHttpClient>();
            tShockClient.BaseAddress = new Uri(target.ApiUrl);
            var res = await tShockClient.GetAuthenticatedServerStatus(
                target.Token,
                stoppingToken
            );

            Logger.Information("Updating server status for world {WorldName}", res.WorldName);

            TerrariaServerCache.Entry entry = _cache.Update(res.WorldName, res);

            await CreateEmbed(entry);
        }
        catch (Exception e)
        {
            Logger.Warning(e, $"Failed to execute scrape for {target.Name} at {target.ApiUrl}");
        }
    }

    private async Task CreateEmbed(TerrariaServerCache.Entry entry)
    {
        const ulong channelId = 526632816151101440ul;
        try
        {
            var guild = _discordClient.GetGuild(GuildId);
            if (guild == null)
            {
                Logger.Warning("Could not find guild by ID: waiting until next scrape");
                return;
            }

            var channel = guild.GetTextChannel(channelId);

            if (channel == null)
            {
                Logger.Debug("Could not find text channel by ID: {ChannelId}", channelId);
                return;
            }
            
            var result = await _embedRepository.GetExistingMessageIdAsync(GuildId, entry.WorldName);

            var entryHashCode = entry.GetHashCode();
            if (result.HasValue && entryHashCode == result.Value.Hash)
            {
                Logger.Debug("Not updating message because the hashes are equivalent");
                return;
            }

            var embed = new EmbedBuilder();

            embed
                .WithTitle($"Terraria Server \"{entry.WorldName}\"")
                .AddField(
                    "Summary",
                    $"home.myam.tech:{entry.Port}"
                );
            
            if (entry.Players.Count > 0)
            {
                embed
                    .AddField("Online Players",
                        string.Join("\n", entry.Players.Select(x => x.Username))
                    )
                    .WithFooter(footer => footer.Text = $"Players {entry.Players.Count}/{entry.MaxPlayers}")
                    .WithCurrentTimestamp();
            }

            if (result.HasValue)
            {
                Logger.Debug("Updating discord message because found it may exist already");
                IUserMessage? modifiedMessage = await channel.ModifyMessageAsync(result.Value.MessageId, (m) =>
                {
                    m.Embed = embed.Build();
                });

                if (modifiedMessage != null)
                {
                    await _embedRepository.SaveMessageIdForEmbedAsync(
                        GuildId,
                        entry.WorldName,
                        modifiedMessage.Id,
                        entryHashCode
                    );
                    return;
                }
                
                Logger.Debug("Message to modify was not found. Falling through on creation");
            }
            
            Logger.Debug("Creating initial embed for server");
            var message = await channel.SendMessageAsync(embed: embed.Build());
            await _embedRepository.SaveMessageIdForEmbedAsync(
                GuildId,
                entry.WorldName,
                message.Id,
                entryHashCode
            );
        }
        catch (Exception e)
        {
            Logger.Warning(e, "Failed to create message");
        }
    }
}