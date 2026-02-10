using System.ComponentModel.DataAnnotations;

namespace OpenMods.Shared.Models;

public enum DeveloperRole
{
    Developer,
    Admin
}

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
    
    public DeveloperRole Role { get; set; } = DeveloperRole.Developer;
    public bool IsBanned { get; set; }
    
    public List<Mod> Mods { get; set; } = new();
    public List<ApiKey> ApiKeys { get; set; } = new();
}
