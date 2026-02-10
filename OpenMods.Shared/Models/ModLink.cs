namespace OpenMods.Shared.Models;

public class ModLink
{
    public int Id { get; set; }
    public int ModId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Url { get; set; } = string.Empty;
}
