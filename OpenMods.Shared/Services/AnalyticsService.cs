using Microsoft.EntityFrameworkCore;
using OpenMods.Shared.Data;
using OpenMods.Shared.Models;

namespace OpenMods.Shared.Services;

public class AnalyticsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AnalyticsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AnalyticsSummary> GetSummary(int developerId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var mods = await context.Mods
            .Where(m => m.DeveloperId == developerId)
            .ToListAsync();

        return new AnalyticsSummary
        {
            TotalDownloads = mods.Sum(m => m.Downloads),
            TotalPageViews = mods.Sum(m => m.Views),
            UniqueUsers = (int)(mods.Sum(m => m.Downloads) * 0.65) // Demo approximation
        };
    }

    public async Task<List<DailyMetric>> GetHistoricalMetrics(int developerId, int days = 30)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        var startDate = DateTime.UtcNow.AddDays(-days).Date;

        return await context.DailyMetrics
            .Include(dm => dm.Mod)
            .Where(dm => dm.Mod.DeveloperId == developerId && dm.Date >= startDate)
            .OrderBy(dm => dm.Date)
            .ToListAsync();
    }

    public async Task<List<Mod>> GetTopPerformingMods(int developerId, int count = 4)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        return await context.Mods
            .Where(m => m.DeveloperId == developerId)
            .OrderByDescending(m => m.Downloads)
            .Take(count)
            .ToListAsync();
    }

    public async Task SeedDemoData(int developerId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var mods = await context.Mods.Where(m => m.DeveloperId == developerId).ToListAsync();
        if (!mods.Any()) return;

        // Check if data already exists
        if (await context.DailyMetrics.AnyAsync(dm => mods.Select(m => m.Id).Contains(dm.ModId)))
            return;

        var random = new Random();
        var now = DateTime.UtcNow.Date;

        foreach (var mod in mods)
        {
            mod.Downloads = random.Next(5000, 50000);
            mod.Views = random.Next(mod.Downloads, mod.Downloads * 4);
            
            for (int i = 30; i >= 0; i--)
            {
                var date = now.AddDays(-i);
                context.DailyMetrics.Add(new DailyMetric
                {
                    ModId = mod.Id,
                    Date = date,
                    Downloads = random.Next(50, 500),
                    Views = random.Next(200, 1500)
                });
            }
        }

        await context.SaveChangesAsync();
    }
}

public class AnalyticsSummary
{
    public int TotalDownloads { get; set; }
    public int TotalPageViews { get; set; }
    public int UniqueUsers { get; set; }
}
