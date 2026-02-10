using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenMods.Shared.Models;
using Markdig;
using System.Text;

namespace OpenMods.Shared.Services;

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

    public async Task<List<GitHubRelease>> GetReleases(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return new List<GitHubRelease>();

        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{fullName}/releases");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(content, _jsonOptions);
                return releases ?? new List<GitHubRelease>();
            }

            _logger.LogWarning("Failed to fetch releases for {FullName}: {StatusCode}", fullName, response.StatusCode);
            return new List<GitHubRelease>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching releases for {FullName}", fullName);
            return new List<GitHubRelease>();
        }
    }

    public async Task<List<GitHubContent>> GetRepositoryContent(string fullName, string path = "")
    {
        if (string.IsNullOrEmpty(fullName)) return new List<GitHubContent>();

        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{fullName}/contents/{path.TrimStart('/')}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<GitHubContent>>(content, _jsonOptions);
                return items ?? new List<GitHubContent>();
            }

            _logger.LogWarning("Failed to fetch content for {FullName}/{Path}: {StatusCode}", fullName, path, response.StatusCode);
            return new List<GitHubContent>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content for {FullName}/{Path}", fullName, path);
            return new List<GitHubContent>();
        }
    }
    public async Task<string?> GetRawFileContent(string fullName, string path)
    {
        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(path)) return null;

        try
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{fullName}/contents/{path.TrimStart('/')}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var fileData = JsonSerializer.Deserialize<GitHubReadme>(content, _jsonOptions);

                if (fileData != null && !string.IsNullOrEmpty(fileData.Content))
                {
                    var base64 = fileData.Content.Replace("\n", "").Replace("\r", "");
                    return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching raw file content for {FullName}/{Path}", fullName, path);
            return null;
        }
    }
}

public class GitHubReadme
{
    public string Content { get; set; } = string.Empty;
    public string Encoding { get; set; } = string.Empty;
}

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    
    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }
    
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
    
    public List<GitHubAsset> Assets { get; set; } = new();
}

public class GitHubAsset
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    
    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;
}

public class GitHubContent
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "file" or "dir"
    
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}
