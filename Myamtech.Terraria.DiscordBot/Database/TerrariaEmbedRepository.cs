namespace Myamtech.Terraria.DiscordBot.Database;

public class TerrariaEmbedRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TerrariaEmbedRepository(
        IServiceScopeFactory scopeFactory
    )
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<(ulong MessageId, int Hash)?> GetExistingMessageIdAsync(long guildId, string worldName)
    {
        using var scope = _scopeFactory.CreateScope();
        await using var database = scope.ServiceProvider.GetRequiredService<DiscordBotDataContext>();
        TerrariaEmbedMessage? embed = await database.Embeds.FindAsync(guildId, worldName).AsTask();
        if (embed == null)
        {
            return null;
        }

        return (embed.MessageId, embed.Hash);
    }
    
    public async Task SaveMessageIdForEmbedAsync(
        long guildId, 
        string worldName,
        ulong messageId,
        int hash
    )
    {
        using var scope = _scopeFactory.CreateScope();
        await using var database = scope.ServiceProvider.GetRequiredService<DiscordBotDataContext>();
        var embed = new TerrariaEmbedMessage()
        {
            WorldName = worldName,
            GuildId = guildId,
            MessageId = messageId,
            Hash = hash
        };

        try
        {
            await database.Embeds.AddAsync(embed).AsTask();
            await database.SaveChangesAsync();
        }
        catch
        {
            database.Embeds.Update(embed);
            await database.SaveChangesAsync();
        }
    }
}