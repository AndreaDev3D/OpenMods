namespace OpenMods.Shared.Models;

public class ModResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LongDescription { get; set; }
    public string? ImageUrl { get; set; }
    public string? GitHubRepoUrl { get; set; }
    public int DeveloperId { get; set; }
    public string? DeveloperName { get; set; }
    public int Stars { get; set; }
    public int Views { get; set; }
    public int Downloads { get; set; }
    public string? InstallJson { get; set; }
    public List<ReleaseResponse> Releases { get; set; } = new();
}
