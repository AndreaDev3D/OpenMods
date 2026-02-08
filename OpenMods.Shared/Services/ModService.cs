using Microsoft.EntityFrameworkCore;
using OpenMods.Shared.Data;
using OpenMods.Shared.Models;
using Microsoft.Extensions.Logging;

namespace OpenMods.Shared.Services;

public class ModService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly GitHubService _githubService;
    private readonly ILogger<ModService> _logger;

    public ModService(IDbContextFactory<AppDbContext> dbFactory, GitHubService githubService, ILogger<ModService> logger)
    {
        _dbFactory = dbFactory;
        _githubService = githubService;
        _logger = logger;
    }

    public async Task<bool> RefreshModFromGitHub(int modId)
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var dbMod = await context.Mods
                .Include(m => m.Releases)
                .ThenInclude(r => r.Assets)
                .FirstOrDefaultAsync(m => m.Id == modId);

            if (dbMod == null) return false;

            var uri = new Uri(dbMod.GitHubRepoUrl);
            var fullName = uri.AbsolutePath.TrimStart('/');

            var repoData = await _githubService.GetRepository(fullName);
            if (repoData != null)
            {
                dbMod.Stars = repoData.Stars;
                dbMod.Description = repoData.Description;
                
                var readmeHtml = await _githubService.GetReadmeHtml(fullName);
                if (!string.IsNullOrEmpty(readmeHtml))
                {
                    dbMod.LongDescription = readmeHtml;
                }

                // Sync Releases
                var githubReleases = await _githubService.GetReleases(fullName);
                if (githubReleases.Any())
                {
                    foreach (var ghRelease in githubReleases)
                    {
                        var release = dbMod.Releases.FirstOrDefault(r => r.Version == ghRelease.TagName);
                        if (release == null)
                        {
                            release = new Release
                            {
                                ModId = dbMod.Id,
                                Version = ghRelease.TagName,
                                ReleasedAt = ghRelease.PublishedAt,
                                HtmlUrl = ghRelease.HtmlUrl,
                            };
                            context.Releases.Add(release);
                        }
                        else
                        {
                            release.ReleasedAt = ghRelease.PublishedAt;
                            release.HtmlUrl = ghRelease.HtmlUrl;
                        }

                        // Sync Assets
                        foreach (var ghAsset in ghRelease.Assets)
                        {
                            var asset = release.Assets.FirstOrDefault(a => a.Name == ghAsset.Name);
                            if (asset == null)
                            {
                                asset = new ReleaseAsset
                                {
                                    Name = ghAsset.Name,
                                    Size = ghAsset.Size,
                                    DownloadUrl = ghAsset.BrowserDownloadUrl,
                                    ContentType = ghAsset.ContentType
                                };
                                release.Assets.Add(asset);
                            }
                            else
                            {
                                asset.Size = ghAsset.Size;
                                asset.DownloadUrl = ghAsset.BrowserDownloadUrl;
                                asset.ContentType = ghAsset.ContentType;
                            }
                        }
                    }
                }

                dbMod.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing mod {ModId} from GitHub", modId);
            return false;
        }
    }
}
