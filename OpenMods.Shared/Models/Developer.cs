using System.ComponentModel.DataAnnotations;

namespace OpenMods.Shared.Models;

public class Developer
{
    public int Id { get; set; }
    
    [Required]
    public string GitHubUsername { get; set; } = string.Empty;
    
    public string? DisplayName { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public List<Mod> Mods { get; set; } = new();
    public List<ApiKey> ApiKeys { get; set; } = new();
}
