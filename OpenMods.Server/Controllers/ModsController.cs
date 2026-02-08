using Microsoft.AspNetCore.Mvc;
using OpenMods.Shared.Services;

namespace OpenMods.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModsController : ControllerBase
{
    private readonly ModService _modService;
    private readonly ILogger<ModsController> _logger;

    public ModsController(ModService modService, ILogger<ModsController> logger)
    {
        _modService = modService;
        _logger = logger;
    }

    [HttpPost("{id}/refresh")]
    public async Task<IActionResult> RefreshMod(int id)
    {
        _logger.LogInformation("API request to refresh mod {ModId}", id);
        
        var success = await _modService.RefreshModFromGitHub(id);
        
        if (success)
        {
            return Ok(new { message = "Mod refreshed successfully" });
        }
        
        return BadRequest(new { message = "Failed to refresh mod. Ensure the mod exists and the GitHub repository is accessible." });
    }
}
