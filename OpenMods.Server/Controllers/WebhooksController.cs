using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMods.Shared.Data;
using OpenMods.Shared.Services;

namespace OpenMods.Server.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ModService _modService;
    private readonly ApiKeyService _apiKeyService;

    public WebhooksController(
        IConfiguration configuration,
        ILogger<WebhooksController> logger,
        IDbContextFactory<AppDbContext> dbFactory,
        ModService modService,
        ApiKeyService apiKeyService)
    {
        _configuration = configuration;
        _logger = logger;
        _dbFactory = dbFactory;
        _modService = modService;
        _apiKeyService = apiKeyService;
    }

    [HttpPost("github")]
    public async Task<IActionResult> HandleGitHubWebhook()
    {
        if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var signature))
        {
            _logger.LogWarning("Missing X-Hub-Signature-256 header");
            return Unauthorized("Missing signature");
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        try
        {
            var payload = JsonSerializer.Deserialize<GitHubWebhookPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (payload?.Repository?.HtmlUrl == null)
            {
                _logger.LogWarning("Webhook payload missing repository URL");
                return BadRequest("Invalid payload");
            }

            var repoUrl = payload.Repository.HtmlUrl;
            _logger.LogInformation("Received webhook for repo: {RepoUrl}", repoUrl);

            using var context = await _dbFactory.CreateDbContextAsync();
            
            // Find mod by repo URL
            var mod = await context.Mods.FirstOrDefaultAsync(m => m.GitHubRepoUrl == repoUrl);

            if (mod == null)
            {
                // Try adding .git suffix or removing it to be robust
                var altUrl = repoUrl.EndsWith(".git") ? repoUrl[..^4] : repoUrl + ".git";
                mod = await context.Mods.FirstOrDefaultAsync(m => m.GitHubRepoUrl == altUrl);
            }

            if (mod == null)
            {
                _logger.LogInformation("No mod found for repo URL: {RepoUrl}", repoUrl);
                return Ok("Ignored: Repo not linked to any mod");
            }

            // Get Developer's API Keys to verify signature
            var apiKeys = await _apiKeyService.GetApiKeysForDeveloper(mod.DeveloperId);
            
            bool isVerified = false;
            foreach (var key in apiKeys)
            {
                if (VerifySignature(body, key.Key, signature))
                {
                    isVerified = true;
                    break;
                }
            }

            if (!isVerified)
            {
                _logger.LogWarning("Invalid GitHub webhook signature. No matching API key found for Developer {DeveloperId}", mod.DeveloperId);
                return Unauthorized("Invalid signature");
            }

            _logger.LogInformation("Refreshing mod {ModId} ({ModName}) triggered by webhook. Verified with API Key.", mod.Id, mod.Name);
            
            // Trigger refresh
            var success = await _modService.RefreshModFromGitHub(mod.Id);

            if (success)
            {
                return Ok($"Refreshed mod {mod.Id}");
            }
            else
            {
                return StatusCode(500, "Failed to refresh mod");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    private bool VerifySignature(string payload, string secret, string signatureWithPrefix)
    {
        if (!signatureWithPrefix.StartsWith("sha256="))
        {
            return false;
        }

        var signature = signatureWithPrefix.Substring(7);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashString),
            Encoding.UTF8.GetBytes(signature));
    }

    public class GitHubWebhookPayload
    {
        public GitHubRepositoryInfo? Repository { get; set; }
    }

    public class GitHubRepositoryInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
