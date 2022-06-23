using Discord.Interactions;
using JetBrains.Annotations;
using Myamtech.Terraria.DiscordBot.Database;

namespace Myamtech.Terraria.DiscordBot.Interactions;

[UsedImplicitly]
public class TestInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordUserRepository _database;

    public TestInteraction(DiscordUserRepository database)
    {
        _database = database;
    }
    
    // [Summary] lets you customize the name and the description of a parameter
    [SlashCommand("echo", "Repeat the input")]
    [UsedImplicitly]
    public async Task Echo(
        string echo,
        [Summary(description: "mention the user")]
        bool mention = false
    )
    {
        await _database.CreateDiscordUser(Context.User.Id.ToString());
        await RespondAsync(echo + (mention ? Context.User.Mention : string.Empty));
    }

}