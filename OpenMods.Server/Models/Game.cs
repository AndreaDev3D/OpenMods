using System.ComponentModel.DataAnnotations;

namespace OpenMods.Server.Models;

public class Game
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public List<Release> Releases { get; set; } = new();
    public List<Mod> Mods { get; set; } = new();
}
