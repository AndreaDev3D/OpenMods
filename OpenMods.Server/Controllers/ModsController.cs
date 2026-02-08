using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMods.Shared.Data;
using OpenMods.Shared.Services;

namespace OpenMods.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApiKeyPolicy")]
public class ModsController : ControllerBase
{
    private readonly ModService _modService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ModsController> _logger;

    public ModsController(ModService modService, IDbContextFactory<AppDbContext> dbFactory, ILogger<ModsController> logger)
    {
        _modService = modService;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    [HttpPost("{id}/refresh")]
    public async Task<IActionResult> RefreshMod(int id)
    {
        var developerIdStr = User.FindFirst("DeveloperId")?.Value;
        if (string.IsNullOrEmpty(developerIdStr) || !int.TryParse(developerIdStr, out var developerId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("API request to refresh mod {ModId} from developer {DeveloperId}", id, developerId);
        
        // Ownership check
        using var context = await _dbFactory.CreateDbContextAsync();
        var mod = await context.Mods.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound(new { message = "Mod not found" });
        }

        if (mod.DeveloperId != developerId)
        {
            _logger.LogWarning("Developer {DeveloperId} attempted to refresh mod {ModId} owned by {OwnerId}", 
                developerId, id, mod.DeveloperId);
            return Forbid();
        }

        var success = await _modService.RefreshModFromGitHub(id);
        
        if (success)
        {
            return Ok(new { message = "Mod refreshed successfully" });
        }
        
        return BadRequest(new { message = "Failed to refresh mod. Ensure the mod exists and the GitHub repository is accessible." });
    }
}
