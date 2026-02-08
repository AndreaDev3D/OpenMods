using System.ComponentModel.DataAnnotations;

namespace OpenMods.Shared.Models;

public class DailyMetric
{
    public int Id { get; set; }
    
    public int ModId { get; set; }
    public Mod Mod { get; set; } = null!;
    
    public DateTime Date { get; set; }
    
    public int Views { get; set; }
    public int Downloads { get; set; }
}
