namespace Myamtech.Terraria.DiscordBot.Database;

public class DiscordUserRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DiscordUserRepository(
        IServiceScopeFactory scopeFactory
    )
    {
        _scopeFactory = scopeFactory;
    }

    public async Task CreateDiscordUser(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        await using var database = scope.ServiceProvider.GetRequiredService<DiscordBotDataContext>();
        await database.Users.AddAsync(new DiscordUser()
        {
            Id = userId
        }).AsTask();
        await database.SaveChangesAsync();
    }
}