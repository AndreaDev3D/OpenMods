using System.Text.Json.Serialization;

namespace OpenMods.Shared.Models;

public class GitHubRepository
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("stargazers_count")]
    public int Stars { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";

    [JsonPropertyName("owner")]
    public GitHubOwner Owner { get; set; } = new();
}

public class GitHubOwner
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = ""; // "User" or "Organization"

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = "";
}
