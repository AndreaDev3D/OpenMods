using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenMods.Shared.Data;
using OpenMods.Shared.Models;
using System.Security.Cryptography;

namespace OpenMods.Shared.Services;

public class ApiKeyService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ApiKeyService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<int?> GetDeveloperIdByHandle(string handle)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        var dev = await context.Developers.AsNoTracking().FirstOrDefaultAsync(d => d.GitHubUsername == handle);
        return dev?.Id;
    }

    public async Task<int?> ValidateApiKey(string key)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        var apiKey = await context.ApiKeys.AsNoTracking().FirstOrDefaultAsync(a => a.Key == key);
        return apiKey?.DeveloperId;
    }

    public async Task UpdateLastUsed(string key)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        var apiKey = await context.ApiKeys.FirstOrDefaultAsync(a => a.Key == key);
        if (apiKey != null)
        {
            apiKey.LastUsedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<ApiKey>> GetApiKeysForDeveloper(int developerId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        return await context.ApiKeys
            .Where(a => a.DeveloperId == developerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApiKey?> GenerateApiKey(int developerId, string name)
    {
        try
        {
            var keyStr = "om_live_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();
            
            var apiKey = new ApiKey
            {
                Name = name,
                Key = keyStr,
                DeveloperId = developerId,
                CreatedAt = DateTime.UtcNow
            };

            using var context = await _dbFactory.CreateDbContextAsync();
            context.ApiKeys.Add(apiKey);
            await context.SaveChangesAsync();
            
            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key for developer {DeveloperId}", developerId);
            return null;
        }
    }

    public async Task<bool> DeleteApiKey(int developerId, int keyId)
    {
        try
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var key = await context.ApiKeys
                .FirstOrDefaultAsync(a => a.Id == keyId && a.DeveloperId == developerId);

            if (key == null) return false;

            context.ApiKeys.Remove(key);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key {KeyId} for developer {DeveloperId}", keyId, developerId);
            return false;
        }
    }
}
