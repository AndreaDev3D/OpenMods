using System.ComponentModel.DataAnnotations;

namespace OpenMods.Server.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Icon { get; set; }
    
    public List<Mod> Mods { get; set; } = new();
}
