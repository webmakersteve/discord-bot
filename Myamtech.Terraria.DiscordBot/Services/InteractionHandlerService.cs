using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;
using Serilog.Context;
using ILogger = Serilog.ILogger;

namespace Myamtech.Terraria.DiscordBot.Services;

public class InteractionHandlerService : IHostedService
{
    private static readonly ILogger Logger = Log.Logger.ForContext<InteractionHandlerService>();
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _socketClient;

    public InteractionHandlerService(
        InteractionService interactionService,
        IServiceProvider serviceProvider,
        DiscordSocketClient socketClient
    )
    {
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _socketClient = socketClient;
        
        socketClient.Ready += async () =>
        {
            Logger.Debug("Registering slash commands to test guild");
            await _interactionService.RegisterCommandsToGuildAsync(259438654890573839, true);
        };
        socketClient.InteractionCreated += HandleInteraction;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        using var _ = LogContext.PushProperty("GuildId", interaction.GuildId);
        using var __ = LogContext.PushProperty("Username", interaction.User.Username);
        
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(_socketClient, interaction);

            // Execute the incoming command.
            Logger.Debug("Handling incoming interaction");
            var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        Logger.Warning("Command had an unmet precondition");
                        break;
                    default:
                        break;
                }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist.
            // It is a good idea to delete the original response, or at least let the user know that
            // something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}