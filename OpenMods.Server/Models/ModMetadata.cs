namespace OpenMods.Server.Models;

public class ModMetadata
{
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public bool IsConfirmed { get; set; }
}
