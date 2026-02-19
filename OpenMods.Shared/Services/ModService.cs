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

            if (string.IsNullOrEmpty(dbMod.GitHubRepoUrl))
            {
                _logger.LogWarning("Mod {ModId} has no GitHub URL", modId);
                return false;
            }

            var uri = new Uri(dbMod.GitHubRepoUrl);
            var fullName = uri.AbsolutePath.TrimStart('/').TrimEnd('/');

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

                // Sync LINKS.md (now in .openmods/)
                var linksContent = await _githubService.GetRawFileContent(fullName, ".openmods/LINKS.md") 
                                 ?? await _githubService.GetRawFileContent(fullName, ".openmods/LINK.md")
                                 ?? await _githubService.GetRawFileContent(fullName, ".openmods/links.md")
                                 ?? await _githubService.GetRawFileContent(fullName, "LINKS.md");
                
                if (!string.IsNullOrEmpty(linksContent))
                {
                    var links = ParseLinks(linksContent, dbMod.Id);
                    if (links.Any())
                    {
                        // Remove old links
                        var oldLinks = await context.ModLinks.Where(l => l.ModId == dbMod.Id).ToListAsync();
                        context.ModLinks.RemoveRange(oldLinks);
                        
                        // Add new links
                        context.ModLinks.AddRange(links);
                    }
                }

                // Sync FAQ.md (now in .openmods/)
                var faqContent = await _githubService.GetRawFileContent(fullName, ".openmods/FAQ.md")
                               ?? await _githubService.GetRawFileContent(fullName, ".openmods/faq.md")
                               ?? await _githubService.GetRawFileContent(fullName, "FAQ.md");

                if (!string.IsNullOrEmpty(faqContent))
                {
                    var faqs = ParseFaqs(faqContent, dbMod.Id);
                    if (faqs.Any())
                    {
                        // Remove old FAQs
                        var oldFaqs = await context.ModFaqs.Where(f => f.ModId == dbMod.Id).ToListAsync();
                        context.ModFaqs.RemoveRange(oldFaqs);

                        // Add new FAQs
                        context.ModFaqs.AddRange(faqs);
                    }
                }

                // Sync install.json
                var installJson = await _githubService.GetRawFileContent(fullName, ".openmods/install.json")
                                ?? await _githubService.GetRawFileContent(fullName, "install.json");
                
                if (!string.IsNullOrEmpty(installJson))
                {
                    dbMod.InstallJson = installJson;
                }

                // Sync Images from .openmods/img/
                var images = await _githubService.GetRepositoryContent(fullName, ".openmods/img");
                if (images.Any())
                {
                    // Update gallery with all images from the folder if it was empty
                    if (dbMod.GalleryImageUrls == null || !dbMod.GalleryImageUrls.Any())
                    {
                        dbMod.GalleryImageUrls = images
                            .Where(i => i.Type == "file" && (i.Name.EndsWith(".png") || i.Name.EndsWith(".jpg") || i.Name.EndsWith(".jpeg") || i.Name.EndsWith(".webp")))
                            .Select(i => i.DownloadUrl)
                            .Where(url => url != null)
                            .Cast<string>()
                            .ToList();
                    }

                    // Auto-detect thumbnail if not set or if it's currently a default
                    if (string.IsNullOrEmpty(dbMod.ImageUrl) || dbMod.ImageUrl.Contains("placeholder"))
                    {
                        var thumbnail = images.FirstOrDefault(i => 
                            i.Name.ToLower().Contains("icon") || 
                            i.Name.ToLower().Contains("logo") || 
                            i.Name.ToLower().Contains("thumbnail") ||
                            i.Name.ToLower().Contains("cover"));
                        
                        if (thumbnail != null && !string.IsNullOrEmpty(thumbnail.DownloadUrl))
                        {
                            dbMod.ImageUrl = thumbnail.DownloadUrl;
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
    public async Task<bool> UpdateModGallery(int modId, List<string> imageUrls)
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var dbMod = await context.Mods.FirstOrDefaultAsync(m => m.Id == modId);
            if (dbMod == null) return false;

            dbMod.GalleryImageUrls = imageUrls;
            dbMod.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gallery for mod {ModId}", modId);
            return false;
        }
    }

    public async Task<bool> UpdateModThumbnail(int modId, string imageUrl)
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var dbMod = await context.Mods.FirstOrDefaultAsync(m => m.Id == modId);
            if (dbMod == null) return false;

            dbMod.ImageUrl = imageUrl;
            dbMod.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating thumbnail for mod {ModId}", modId);
            return false;
        }
    }

    private List<ModLink> ParseLinks(string content, int modId)
    {
        var links = new List<ModLink>();
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            // Support: [icon:name] [Label](Url) or just [Label](Url)
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:\[icon:([^\]]+)\]\s*)?\[([^\]]+)\]\(([^)]+)\)");
            if (match.Success)
            {
                links.Add(new ModLink
                {
                    ModId = modId,
                    Icon = match.Groups[1].Success ? match.Groups[1].Value.Trim() : null,
                    Label = match.Groups[2].Value.Trim(),
                    Url = match.Groups[3].Value.Trim()
                });
            }
        }
        
        return links;
    }

    private List<ModFaq> ParseFaqs(string content, int modId)
    {
        var faqs = new List<ModFaq>();
        // Match ### followed by question, then any content (answer) until next ### or end of string
        var regex = new System.Text.RegularExpressions.Regex(@"(?m)^###\s*(.+?)\s*\n(.*?)(?=\n^###|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
        var matches = regex.Matches(content);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var question = match.Groups[1].Value.Trim();
            var answer = match.Groups[2].Value.Trim();

            if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
            {
                faqs.Add(new ModFaq
                {
                    ModId = modId,
                    Question = question,
                    Answer = answer
                });
            }
        }

        return faqs;
    }
}
