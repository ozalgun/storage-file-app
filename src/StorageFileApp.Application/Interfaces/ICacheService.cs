using StorageFileApp.Application.DTOs;

namespace StorageFileApp.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
    Task<IEnumerable<string>> GetKeysAsync(string pattern);
}

public interface IFileCacheService : ICacheService
{
    Task<IEnumerable<FileDTO>?> GetFileListAsync(string cacheKey);
    Task SetFileListAsync(string cacheKey, IEnumerable<FileDTO> files, TimeSpan? expiration = null);
    Task InvalidateFileListCacheAsync();
    Task InvalidateFileCacheAsync(Guid fileId);
}
