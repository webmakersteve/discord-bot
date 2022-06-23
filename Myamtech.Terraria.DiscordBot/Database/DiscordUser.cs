using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myamtech.Terraria.DiscordBot.Database;

public class DiscordUser
{
    [Key]
    [Column(Order = 0)]
    public string Id { get; set; } = null!;

}