using System.ComponentModel.DataAnnotations;

namespace OpenMods.Shared.Models;

public class ApiKey
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Key { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public int DeveloperId { get; set; }
    public Developer Developer { get; set; } = null!;
}
