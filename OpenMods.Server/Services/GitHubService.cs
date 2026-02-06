using System.Net.Http.Headers;
using System.Text.Json;
using OpenMods.Server.Models;

namespace OpenMods.Server.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(HttpClient httpClient, ILogger<GitHubService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // GitHub API requires a User-Agent
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OpenMods", "1.0"));
    }

    public async Task<List<GitHubRepository>> GetPublicRepositories(string githubHandle)
    {
        if (string.IsNullOrEmpty(githubHandle))
        {
            return new List<GitHubRepository>();
        }

        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/users/{githubHandle}/repos?type=public&sort=updated");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var repos = JsonSerializer.Deserialize<List<GitHubRepository>>(content);
                return repos ?? new List<GitHubRepository>();
            }
            
            _logger.LogWarning("Failed to fetch repositories for {Handle}: {StatusCode}", githubHandle, response.StatusCode);
            return new List<GitHubRepository>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repositories for {Handle}", githubHandle);
            return new List<GitHubRepository>();
        }
    }
}
