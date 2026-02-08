using System.ComponentModel.DataAnnotations;

namespace OpenMods.Shared.Models;

public class Mod
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? LongDescription { get; set; }

    public string? ImageUrl { get; set; }

    public string? GitHubRepoUrl { get; set; }

    public int DeveloperId { get; set; }
    public Developer? Developer { get; set; }

    public List<Release> Releases { get; set; } = new();

    public List<Category> Categories { get; set; } = new();

    public List<string>? Tags { get; set; } = new();
    public List<Game> SupportedGames { get; set; } = new();

    public int Stars { get; set; }
    public int Views { get; set; }
    public int Downloads { get; set; }

    public List<DailyMetric> DailyMetrics { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsArchived { get; set; } = false;
}


