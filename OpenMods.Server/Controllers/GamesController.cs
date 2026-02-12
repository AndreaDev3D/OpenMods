using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMods.Shared.Data;
using OpenMods.Shared.Models;

namespace OpenMods.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApiKeyPolicy")]
public class GamesController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<GamesController> _logger;

    public GamesController(IDbContextFactory<AppDbContext> dbFactory, ILogger<GamesController> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetGames()
    {
        _logger.LogInformation("API request to get all supported games");
        using var context = await _dbFactory.CreateDbContextAsync();
        var games = await context.Games
            .AsNoTracking()
            .Select(g => new GameResponse
            {
                Id = g.Id,
                Name = g.Name,
                ImageUrl = g.ImageUrl,
                Description = g.Description,
                ModsCount = g.Mods.Count
            })
            .ToListAsync();
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GameResponse>> GetGameById(int id)
    {
        _logger.LogInformation("API request to get game by id {GameId}", id);
        using var context = await _dbFactory.CreateDbContextAsync();
        var game = await context.Games
            .AsNoTracking()
            .Select(g => new GameResponse
            {
                Id = g.Id,
                Name = g.Name,
                ImageUrl = g.ImageUrl,
                Description = g.Description,
                ModsCount = g.Mods.Count
            })
            .FirstOrDefaultAsync(g => g.Id == id);

        if (game == null)
        {
            return NotFound(new { message = "Game not found" });
        }

        return Ok(game);
    }

    [HttpGet("{gameId}/mods/{modId}")]
    public async Task<ActionResult<ModResponse>> GetModByIdByGameId(int gameId, int modId)
    {
        _logger.LogInformation("API request to get mod {ModId} for game {GameId}", modId, gameId);
        using var context = await _dbFactory.CreateDbContextAsync();
        
        var mod = await context.Mods
            .AsNoTracking()
            .Where(m => m.Id == modId && m.SupportedGames.Any(g => g.Id == gameId))
            .Select(m => new ModResponse
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                LongDescription = m.LongDescription,
                ImageUrl = m.ImageUrl,
                GitHubRepoUrl = m.GitHubRepoUrl,
                DeveloperId = m.DeveloperId,
                DeveloperName = m.Developer != null ? m.Developer.DisplayName ?? m.Developer.GitHubUsername : null,
                Stars = m.Stars,
                Views = m.Views,
                Downloads = m.Downloads,
                Releases = m.Releases
                    .OrderByDescending(r => r.ReleasedAt)
                    .Select(r => new ReleaseResponse
                    {
                        Id = r.Id,
                        Version = r.Version,
                        DownloadUrl = r.DownloadUrl,
                        HtmlUrl = r.HtmlUrl,
                        Changelog = r.Changelog,
                        ReleasedAt = r.ReleasedAt,
                        Assets = r.Assets.Select(a => new ReleaseAssetResponse
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Size = a.Size,
                            DownloadUrl = a.DownloadUrl,
                            ContentType = a.ContentType,
                            DownloadCount = a.DownloadCount
                        }).ToList()
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (mod == null)
        {
            return NotFound(new { message = "Mod not found for the specified game" });
        }

        return Ok(mod);
    }
}
