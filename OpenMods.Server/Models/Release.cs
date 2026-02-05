using System.ComponentModel.DataAnnotations;

namespace OpenMods.Server.Models;

public class Release
{
    public int Id { get; set; }
    
    public int ModId { get; set; }
    public Mod? Mod { get; set; }
    
    [Required]
    public string Version { get; set; } = string.Empty;
    
    public string? DownloadUrl { get; set; }
    
    public string? Changelog { get; set; }
    
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
}
