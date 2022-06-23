using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myamtech.Terraria.DiscordBot.Database;

public class TerrariaEmbedMessage
{
    [Column(Order = 0)]
    public long GuildId { get; set; }
    
    [Column(Order = 1)]
    public string WorldName { get; set; } = null!;
    
    [Column(Order = 3)]
    public ulong MessageId { get; set; }
    
    [Column(Order = 3)]
    public int Hash { get; set; }

}