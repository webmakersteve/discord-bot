using System.ComponentModel.DataAnnotations;

namespace Myamtech.Terraria.DiscordBot.Configuration;

public class TerrariaTargetsConfiguration : IValidatableObject
{
    public class TerrariaTarget
    {
        [Required] public string ApiUrl { get; set; } = null!;

        [Required] public string Name { get; set; } = null!;

        [Required] public string Token { get; set; } = null!;
    }
    
    public List<TerrariaTarget> Targets { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in Targets)
        {
            if (string.IsNullOrEmpty(target.ApiUrl))
            {
                yield return new ValidationResult("Must provide an API url for each target");
            }

            if (string.IsNullOrEmpty(target.Name))
            {
                yield return new ValidationResult("Must provide a name");
            }

            if (!names.Add(target.Name))
            {
                yield return new ValidationResult("All names must be unique");
            }
        }
        
        
    }
}