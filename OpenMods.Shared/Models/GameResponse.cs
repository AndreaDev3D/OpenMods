namespace OpenMods.Shared.Models;

public class GameResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int ModsCount { get; set; }
}
