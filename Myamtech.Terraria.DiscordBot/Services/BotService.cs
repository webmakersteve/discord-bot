using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Myamtech.Terraria.DiscordBot.Configuration;
using Myamtech.Terraria.DiscordBot.Terraria;
using Prometheus;
using Serilog;
using Serilog.Context;
using ILogger = Serilog.ILogger;

namespace Myamtech.Terraria.DiscordBot.Services;

public class BotService : BackgroundService
{
    #region Metrics
    private static readonly IGauge MetricsJoinedGuilds = Metrics.CreateGauge(
        "terraria_discord_joined_guilds", 
        "Number of guilds the bot has joined", 
        new GaugeConfiguration()
        {
        }
    );
    private static readonly Counter MetricsObservedMessages = Metrics.CreateCounter(
        "terraria_discord_observed_messages", 
        "Number of message observed", 
        new CounterConfiguration()
        {
            LabelNames = new []{ "guild_id", "channel_id" }
        }
    );
    #endregion
    
    private static readonly ILogger Logger = Log.Logger.ForContext<BotService>();
    private readonly DiscordSocketClient _discordClient;
    private readonly IOptions<DiscordConfiguration> _config;
    private readonly TerrariaServerCache _terrariaServerCache;

    public BotService(
        DiscordSocketClient discordClient,
        IOptions<DiscordConfiguration> discordBotConfig,
        TerrariaServerCache serverCache
    )
    {
        _discordClient = discordClient;
        _config = discordBotConfig;
        _terrariaServerCache = serverCache;
        
        _discordClient.Log += OnLogMessage;
        _discordClient.GuildAvailable += OnGuildAvailable;
        _discordClient.GuildUnavailable += OnGuildUnavailable;
        _discordClient.MessageReceived += OnMessageReceived;
        
        _terrariaServerCache.OnUpdate += OnTerrariaServerUpdate;
    }

    private void OnTerrariaServerUpdate(object? sender, TerrariaServerCache.Entry entry)
    {
    }

    private Task OnMessageReceived(SocketMessage arg)
    {
        if (!(arg is SocketUserMessage message))
        {
            return Task.CompletedTask;
        }
        
        if (message.Channel is SocketGuildChannel guildChannel)
        {
            MetricsObservedMessages
                .WithLabels(guildChannel.Guild.Name, arg.Channel.Name)
                .Inc();
        }

        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.Information("Logging in as bot...");
        
        try
        {
            await _discordClient.LoginAsync(TokenType.Bot, _config.Value.BotToken).WaitAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Logger.Warning("Login interrupted by cancellation");
        }
        catch (Exception e)
        {
            Logger.Fatal(e, "Failed to log in with provided credentials");
            throw;
        }

        try
        {
            await _discordClient.StartAsync();

        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Logger.Warning("Start interrupted by cancellation");
        }
        catch (Exception e)
        {
            Logger.Fatal(e, "Failed to start discord client listener");
            throw;
        }
    }

    private Task OnGuildAvailable(SocketGuild arg)
    {
        Logger.Information("New guild has been made available: {GuildName} [{GuildId}]", arg.Name, arg.Id);
        MetricsJoinedGuilds.Set(_discordClient.Guilds.Count);

        return Task.CompletedTask;
    }

    private Task OnGuildUnavailable(SocketGuild arg)
    {
        Logger.Information("Guild is no longer available: {GuildName}", arg.Name);
        MetricsJoinedGuilds.Set(_discordClient.Guilds.Count);
        return Task.CompletedTask;
    }

    private Task OnLogMessage(LogMessage arg)
    {
        using var _ = LogContext.PushProperty("DiscordLogSource", arg.Source);
        if (arg.Exception != null)
        {
            Logger.Warning(arg.Exception, arg.Message ?? "An exception has occurred");
            return Task.CompletedTask;
        }
        
        Logger.Information(arg.Message);
        return Task.CompletedTask;
    }
}