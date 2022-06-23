using Discord;
using Discord.WebSocket;
using Myamtech.Terraria.DiscordBot.Database;
using Myamtech.Terraria.DiscordBot.Terraria;
using Serilog;
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
            var tShockClient = scope.ServiceProvider.GetRequiredService<TShockHttpClient>();
            tShockClient.BaseAddress = new Uri("http://localhost:7878");
            var res = await tShockClient.GetAuthenticatedServerStatus(
                "SCRAPER_TOKEN", 
                stoppingToken
            );

            
            Logger.Information("Updating server status for world {WorldName}", res.WorldName);
            
            TerrariaServerCache.Entry entry = _cache.Update(res.WorldName, res);

            await CreateEmbed(entry);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task CreateEmbed(TerrariaServerCache.Entry entry)
    {
        try
        {
            var guild = _discordClient.GetGuild(GuildId);
            if (guild == null)
            {
                Logger.Warning("Could not find guild by ID: waiting until next scrape");
                return;
            }

            var channel = guild.GetTextChannel(526632816151101440ul);
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