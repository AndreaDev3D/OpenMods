namespace OpenMods.Shared.Models;

public class ReleaseAssetResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public int DownloadCount { get; set; }
}
