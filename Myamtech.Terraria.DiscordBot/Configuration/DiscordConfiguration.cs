using System.ComponentModel.DataAnnotations;

namespace Myamtech.Terraria.DiscordBot.Configuration;

public class DiscordConfiguration : IValidatableObject
{
    [Required]
    public string? BotToken { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(BotToken))
        {
            yield return new ValidationResult($"{nameof(BotToken)} must be set");
        }
    }
}