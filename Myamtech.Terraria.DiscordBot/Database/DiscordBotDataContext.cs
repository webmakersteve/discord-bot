using Microsoft.EntityFrameworkCore;

namespace Myamtech.Terraria.DiscordBot.Database;

public class DiscordBotDataContext : DbContext
{
    public DbSet<DiscordUser> Users { get; set; } = null!;
    public DbSet<TerrariaEmbedMessage> Embeds { get; set; } = null!;

    public DiscordBotDataContext(DbContextOptions<DiscordBotDataContext> contextOptions) : base(contextOptions)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscordUser>().ToTable("users");
        modelBuilder.Entity<TerrariaEmbedMessage>()
            .ToTable("embeds")
            .HasKey(c => new {c.GuildId, c.WorldName});
    }
    
}