using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using System.Text.Json;

namespace StorageFileApp.Infrastructure.Services;

public class RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDistributedCache _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
    private readonly ILogger<RedisCacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            _logger.LogDebug("Getting cache for key: {Key}", key);
            
            var cachedValue = await _distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            _logger.LogDebug("Setting cache for key: {Key}", key);
            
            var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                // Default expiration: 1 hour
                options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            }

            await _distributedCache.SetStringAsync(key, jsonValue, options);
            _logger.LogDebug("Cache set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            _logger.LogDebug("Removing cache for key: {Key}", key);
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            _logger.LogDebug("Removing cache by pattern: {Pattern}", pattern);
            // Note: Redis pattern matching requires server-side implementation
            // For now, we'll log the pattern - in production, you'd use Redis SCAN
            _logger.LogWarning("Pattern-based cache removal not implemented for pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
        }
        
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key);
            return !string.IsNullOrEmpty(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        try
        {
            _logger.LogDebug("Getting cache keys for pattern: {Pattern}", pattern);
            // Note: Redis key enumeration requires server-side implementation
            // For now, return empty - in production, you'd use Redis SCAN
            _logger.LogWarning("Key enumeration not implemented for pattern: {Pattern}", pattern);
            return Task.FromResult(Enumerable.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache keys for pattern: {Pattern}", pattern);
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }
}
