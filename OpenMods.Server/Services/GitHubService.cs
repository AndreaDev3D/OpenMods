using System.Net.Http.Headers;
using System.Text.Json;
using OpenMods.Server.Models;
using Markdig;
using System.Text;

namespace OpenMods.Server.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

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
                var repos = JsonSerializer.Deserialize<List<GitHubRepository>>(content, _jsonOptions);
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

    public async Task<GitHubRepository?> GetRepository(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return null;

        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{fullName}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GitHubRepository>(content, _jsonOptions);
            }

            _logger.LogWarning("Failed to fetch repository {FullName}: {StatusCode}", fullName, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repository {FullName}", fullName);
            return null;
        }
    }

    public async Task<string?> GetReadmeHtml(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return null;

        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{fullName}/readme");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var readmeData = JsonSerializer.Deserialize<GitHubReadme>(content, _jsonOptions);

                if (readmeData != null && !string.IsNullOrEmpty(readmeData.Content))
                {
                    _logger.LogInformation("Successfully fetched README for {FullName}, length: {Length}", fullName, readmeData.Content.Length);
                    // GitHub content is Base64 encoded often with newlines
                    var base64 = readmeData.Content.Replace("\n", "").Replace("\r", "");
                    var markdown = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

                    // Convert Markdown to HTML
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .UseEmojiAndSmiley()
                        .Build();
                    return Markdown.ToHtml(markdown, pipeline);
                }
            }

            _logger.LogWarning("Failed to fetch README for {FullName}: {StatusCode}", fullName, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching README for {FullName}", fullName);
            return null;
        }
    }
}

public class GitHubReadme
{
    public string Content { get; set; } = string.Empty;
    public string Encoding { get; set; } = string.Empty;
}
