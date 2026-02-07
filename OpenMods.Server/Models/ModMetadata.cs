namespace OpenMods.Server.Models;

public class ModMetadata
{
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<int> SelectedGameIds { get; set; } = new();
    public bool IsConfirmed { get; set; }
}
