using Microsoft.Extensions.Logging;
using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.Interfaces;

namespace StorageFileApp.Infrastructure.Services;

public class FileCacheService(ICacheService cacheService, ILogger<FileCacheService> logger) : IFileCacheService
{
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<FileCacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    private const string FILE_LIST_CACHE_KEY = "file_list";
    private const string FILE_CACHE_KEY_PREFIX = "file_";
    private static readonly TimeSpan DefaultFileListExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultFileExpiration = TimeSpan.FromHours(1);

    public async Task<IEnumerable<FileDTO>?> GetFileListAsync(string cacheKey)
    {
        try
        {
            _logger.LogDebug("Getting file list from cache with key: {CacheKey}", cacheKey);
            return await _cacheService.GetAsync<IEnumerable<FileDTO>>(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file list from cache with key: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetFileListAsync(string cacheKey, IEnumerable<FileDTO> files, TimeSpan? expiration = null)
    {
        try
        {
            _logger.LogDebug("Setting file list in cache with key: {CacheKey}, Count: {Count}", cacheKey, files.Count());
            await _cacheService.SetAsync(cacheKey, files.ToList(), expiration ?? DefaultFileListExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting file list in cache with key: {CacheKey}", cacheKey);
        }
    }

    public async Task InvalidateFileListCacheAsync()
    {
        try
        {
            _logger.LogDebug("Invalidating file list cache");
            await _cacheService.RemoveAsync(FILE_LIST_CACHE_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating file list cache");
        }
    }

    public async Task InvalidateFileCacheAsync(Guid fileId)
    {
        try
        {
            var fileCacheKey = $"{FILE_CACHE_KEY_PREFIX}{fileId}";
            _logger.LogDebug("Invalidating file cache for file ID: {FileId}", fileId);
            await _cacheService.RemoveAsync(fileCacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating file cache for file ID: {FileId}", fileId);
        }
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        return await _cacheService.GetAsync<T>(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        await _cacheService.SetAsync(key, value, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _cacheService.RemoveAsync(key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        await _cacheService.RemoveByPatternAsync(pattern);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _cacheService.ExistsAsync(key);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        return await _cacheService.GetKeysAsync(pattern);
    }
}
