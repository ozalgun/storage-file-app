using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace StorageFileApp.Infrastructure.Services;

public class FileSystemStorageService : IStorageService
{
    private readonly ILogger<FileSystemStorageService> _logger;
    private readonly string _basePath;

    public FileSystemStorageService(ILogger<FileSystemStorageService> logger, string basePath = "storage")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _basePath = basePath;
        
        // Ensure base directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<bool> StoreChunkAsync(FileChunk chunk, byte[] data)
    {
        try
        {
            var filePath = GetChunkFilePath(chunk);
            var directory = Path.GetDirectoryName(filePath);
            
            _logger.LogInformation("Attempting to store chunk {ChunkId} to {FilePath}", chunk.Id, filePath);
            _logger.LogInformation("Base path: {BasePath}, Directory: {Directory}", _basePath, directory);
            
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory: {Directory}", directory);
            }

            await File.WriteAllBytesAsync(filePath, data);
            
            // Verify file was created
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                _logger.LogInformation("Successfully stored chunk {ChunkId} to file system at {FilePath}, Size: {Size} bytes", 
                    chunk.Id, filePath, fileInfo.Length);
            }
            else
            {
                _logger.LogError("File was not created at {FilePath}", filePath);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store chunk {ChunkId} to file system", chunk.Id);
            return false;
        }
    }

    public async Task<byte[]?> RetrieveChunkAsync(FileChunk chunk)
    {
        try
        {
            var filePath = GetChunkFilePath(chunk);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Chunk file not found: {FilePath}", filePath);
                return null;
            }

            var data = await File.ReadAllBytesAsync(filePath);
            _logger.LogInformation("Retrieved chunk {ChunkId} from file system", chunk.Id);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve chunk {ChunkId} from file system", chunk.Id);
            return null;
        }
    }

    public Task<bool> DeleteChunkAsync(FileChunk chunk)
    {
        try
        {
            var filePath = GetChunkFilePath(chunk);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted chunk {ChunkId} from file system", chunk.Id);
            }
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chunk {ChunkId} from file system", chunk.Id);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ChunkExistsAsync(FileChunk chunk)
    {
        var filePath = GetChunkFilePath(chunk);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<long> GetChunkSizeAsync(FileChunk chunk)
    {
        try
        {
            var filePath = GetChunkFilePath(chunk);
            
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return Task.FromResult(fileInfo.Length);
            }
            
            return Task.FromResult(0L);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get size for chunk {ChunkId}", chunk.Id);
            return Task.FromResult(0L);
        }
    }

    public Task<bool> ValidateChunkIntegrityAsync(FileChunk chunk, byte[] data)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var computedHash = Convert.ToHexString(sha256.ComputeHash(data));
            var isValid = computedHash.Equals(chunk.Checksum, StringComparison.OrdinalIgnoreCase);
            
            if (!isValid)
            {
                _logger.LogWarning("Chunk {ChunkId} integrity validation failed. Expected: {Expected}, Computed: {Computed}", 
                    chunk.Id, chunk.Checksum, computedHash);
            }
            
            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate integrity for chunk {ChunkId}", chunk.Id);
            return Task.FromResult(false);
        }
    }

    public Task<IEnumerable<FileChunk>> DistributeChunksAsync(IEnumerable<FileChunk> chunks)
    {
        // For file system storage, all chunks are stored locally
        // This method could be extended to distribute across multiple file system paths
        return Task.FromResult(chunks);
    }

    public Task<bool> ReplicateChunkAsync(FileChunk sourceChunk, StorageProvider targetProvider)
    {
        try
        {
            // For file system storage, replication means copying to another directory
            var sourcePath = GetChunkFilePath(sourceChunk);
            var targetPath = GetChunkFilePath(sourceChunk, targetProvider.Id);
            
            if (File.Exists(sourcePath))
            {
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                
                File.Copy(sourcePath, targetPath, true);
                _logger.LogInformation("Replicated chunk {ChunkId} to provider {ProviderId}", sourceChunk.Id, targetProvider.Id);
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replicate chunk {ChunkId} to provider {ProviderId}", sourceChunk.Id, targetProvider.Id);
            return Task.FromResult(false);
        }
    }

    public async Task<bool> TestProviderConnectionAsync(StorageProvider provider)
    {
        try
        {
            // For file system storage, test if we can create/write to the directory
            var testPath = Path.Combine(_basePath, provider.Id.ToString(), "test");
            Directory.CreateDirectory(testPath);
            
            var testFile = Path.Combine(testPath, "connection_test.txt");
            await File.WriteAllTextAsync(testFile, "Connection test");
            File.Delete(testFile);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connection for provider {ProviderId}", provider.Id);
            return false;
        }
    }

    public Task<long> GetAvailableSpaceAsync(StorageProvider provider)
    {
        try
        {
            var drive = new DriveInfo(_basePath);
            return Task.FromResult(drive.AvailableFreeSpace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available space for provider {ProviderId}", provider.Id);
            return Task.FromResult(0L);
        }
    }

    public Task<bool> IsProviderHealthyAsync(StorageProvider provider)
    {
        return TestProviderConnectionAsync(provider);
    }

    private string GetChunkFilePath(FileChunk chunk, Guid? providerId = null)
    {
        var provider = providerId ?? chunk.StorageProviderId;
        return Path.Combine(_basePath, provider.ToString(), chunk.FileId.ToString(), $"{chunk.Order:D6}.chunk");
    }
}
