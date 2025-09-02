using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Constants;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Extensions;

public static class DomainExtensions
{
    // File extensions
    public static bool IsValidSize(this FileEntity file)
    {
        return file.Size >= DomainConstants.MIN_FILE_SIZE && 
               file.Size <= DomainConstants.MAX_FILE_SIZE;
    }
    
    public static bool IsProcessing(this FileEntity file)
    {
        return file.Status == FileStatus.Processing;
    }
    
    public static bool IsCompleted(this FileEntity file)
    {
        return file.Status == FileStatus.Stored;
    }
    
    public static bool HasError(this FileEntity file)
    {
        return file.Status == FileStatus.Error;
    }

    private static string GetFileExtension(this FileEntity file)
    {
        return Path.GetExtension(file.Name).ToLower();
    }
    
    public static bool IsImageFile(this FileEntity file)
    {
        var extension = file.GetFileExtension();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".svg";
    }
    
    public static bool IsVideoFile(this FileEntity file)
    {
        var extension = file.GetFileExtension();
        return extension is ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm";
    }
    
    public static bool IsDocumentFile(this FileEntity file)
    {
        var extension = file.GetFileExtension();
        return extension is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx";
    }
    
    // Chunk extensions
    private static bool IsHealthy(this FileChunk chunk)
    {
        return chunk is { Status: ChunkStatus.Stored, Size: > 0 } && 
               !string.IsNullOrWhiteSpace(chunk.Checksum);
    }

    private static bool NeedsReplication(this FileChunk chunk)
    {
        return chunk.Status == ChunkStatus.Error || 
               chunk.Status == ChunkStatus.Deleted;
    }
    
    public static bool IsCorrupted(this FileChunk chunk)
    {
        return chunk.Status == ChunkStatus.Error || 
               chunk.Size <= 0 || 
               string.IsNullOrWhiteSpace(chunk.Checksum);
    }

    private static TimeSpan GetAge(this FileChunk chunk)
    {
        return DateTime.UtcNow - chunk.CreatedAt;
    }
    
    public static bool IsStale(this FileChunk chunk, TimeSpan maxAge)
    {
        return chunk.GetAge() > maxAge;
    }
    
    // Storage provider extensions
    public static bool IsOverloaded(this StorageProvider provider, int currentLoad)
    {
        return currentLoad > DomainConstants.MAX_CONCURRENT_OPERATIONS_PER_PROVIDER;
    }

    private static bool IsLocal(this StorageProvider provider)
    {
        return provider.Type == StorageProviderType.FileSystem;
    }

    private static bool IsCloud(this StorageProvider provider)
    {
        return provider.Type == StorageProviderType.CloudStorage;
    }
    
    public static bool IsNetwork(this StorageProvider provider)
    {
        return provider.Type == StorageProviderType.NetworkStorage;
    }
    
    public static bool IsDatabase(this StorageProvider provider)
    {
        return provider.Type == StorageProviderType.Database;
    }

    private static int GetPriority(this StorageProvider provider)
    {
        return provider.Type switch
        {
            StorageProviderType.FileSystem => 1,      // Highest priority
            StorageProviderType.NetworkStorage => 2,   // High priority
            StorageProviderType.Database => 3,         // Medium priority
            StorageProviderType.CloudStorage => 4,     // Lower priority
            _ => 5
        };
    }
    
    // Enum extensions
    public static string GetDisplayName(this FileStatus status)
    {
        return status switch
        {
            FileStatus.Pending => "Pending",
            FileStatus.Processing => "Processing",
            FileStatus.Chunked => "Chunked",
            FileStatus.Stored => "Stored",
            FileStatus.Error => "Error",
            FileStatus.Deleted => "Deleted",
            _ => "Unknown"
        };
    }
    
    public static string GetDisplayName(this ChunkStatus status)
    {
        return status switch
        {
            ChunkStatus.Pending => "Pending",
            ChunkStatus.Storing => "Storing",
            ChunkStatus.Stored => "Stored",
            ChunkStatus.Error => "Error",
            ChunkStatus.Deleted => "Deleted",
            _ => "Unknown"
        };
    }
    
    public static string GetDisplayName(this StorageProviderType type)
    {
        return type switch
        {
            StorageProviderType.FileSystem => "File System",
            StorageProviderType.Database => "Database",
            StorageProviderType.CloudStorage => "Cloud Storage",
            StorageProviderType.NetworkStorage => "Network Storage",
            _ => "Unknown"
        };
    }
    
    // Collection extensions
    public static bool IsComplete(this IEnumerable<FileChunk> chunks)
    {
        var chunksList = chunks.ToList();
        return chunksList.Any() && chunksList.All(c => c.Status == ChunkStatus.Stored);
    }
    
    public static bool HasErrors(this IEnumerable<FileChunk> chunks)
    {
        return chunks.Any(c => c.Status == ChunkStatus.Error);
    }
    
    public static IEnumerable<FileChunk> GetHealthyChunks(this IEnumerable<FileChunk> chunks)
    {
        return chunks.Where(c => c.IsHealthy());
    }
    
    public static IEnumerable<FileChunk> GetUnhealthyChunks(this IEnumerable<FileChunk> chunks)
    {
        return chunks.Where(c => !c.IsHealthy());
    }
    
    public static IEnumerable<FileChunk> GetChunksNeedingReplication(this IEnumerable<FileChunk> chunks)
    {
        return chunks.Where(c => c.NeedsReplication());
    }
    
    public static long GetTotalSize(this IEnumerable<FileChunk> chunks)
    {
        return chunks.Sum(c => c.Size);
    }

    private static IEnumerable<StorageProvider> GetActiveProviders(this IEnumerable<StorageProvider> providers)
    {
        return providers.Where(p => p.IsActive);
    }
    
    public static IEnumerable<StorageProvider> GetLocalProviders(this IEnumerable<StorageProvider> providers)
    {
        return providers.Where(p => p.IsLocal());
    }
    
    public static IEnumerable<StorageProvider> GetCloudProviders(this IEnumerable<StorageProvider> providers)
    {
        return providers.Where(p => p.IsCloud());
    }
    
    public static StorageProvider? GetPrimaryProvider(this IEnumerable<StorageProvider> providers)
    {
        return providers.GetActiveProviders().OrderBy(p => p.GetPriority()).FirstOrDefault();
    }
}
