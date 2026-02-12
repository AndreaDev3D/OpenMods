namespace OpenMods.Shared.Models;

public class ReleaseResponse
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? HtmlUrl { get; set; }
    public string? Changelog { get; set; }
    public DateTime ReleasedAt { get; set; }
    public List<ReleaseAssetResponse> Assets { get; set; } = new();
}
