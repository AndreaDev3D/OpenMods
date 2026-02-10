namespace OpenMods.Shared.Models;

public class ModFaq
{
    public int Id { get; set; }
    public int ModId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
