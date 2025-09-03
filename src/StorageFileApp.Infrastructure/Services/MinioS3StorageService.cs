using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using System.Security.Cryptography;

namespace StorageFileApp.Infrastructure.Services;

public class MinioS3StorageService : IStorageService
{
    private readonly ILogger<MinioS3StorageService> _logger;
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public MinioS3StorageService(
        ILogger<MinioS3StorageService> logger, 
        IAmazonS3 s3Client, 
        string bucketName = "storage-file-app")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _bucketName = bucketName;
        
        // Ensure bucket exists
        _ = Task.Run(EnsureBucketExistsAsync);
    }

    public async Task<bool> StoreChunkAsync(FileChunk chunk, byte[] data)
    {
        try
        {
            var key = GetChunkKey(chunk);
            
            _logger.LogInformation("Storing chunk {ChunkId} to S3 with key {Key}", chunk.Id, key);
            
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = new MemoryStream(data),
                ContentType = "application/octet-stream",
                Metadata = 
                {
                    ["chunk-id"] = chunk.Id.ToString(),
                    ["file-id"] = chunk.FileId.ToString(),
                    ["chunk-order"] = chunk.Order.ToString(),
                    ["chunk-size"] = chunk.Size.ToString(),
                    ["checksum"] = chunk.Checksum
                }
            };

            var response = await _s3Client.PutObjectAsync(request);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully stored chunk {ChunkId} to S3 at {Key}, Size: {Size} bytes", 
                    chunk.Id, key, data.Length);
                return true;
            }
            else
            {
                _logger.LogError("Failed to store chunk {ChunkId} to S3. Status: {StatusCode}", 
                    chunk.Id, response.HttpStatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store chunk {ChunkId} to S3", chunk.Id);
            return false;
        }
    }

    public async Task<byte[]?> RetrieveChunkAsync(FileChunk chunk)
    {
        try
        {
            var key = GetChunkKey(chunk);
            
            _logger.LogInformation("Retrieving chunk {ChunkId} from S3 with key {Key}", chunk.Id, key);
            
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            using var response = await _s3Client.GetObjectAsync(request);
            using var memoryStream = new MemoryStream();
            
            await response.ResponseStream.CopyToAsync(memoryStream);
            var data = memoryStream.ToArray();
            
            _logger.LogInformation("Successfully retrieved chunk {ChunkId} from S3, Size: {Size} bytes", 
                chunk.Id, data.Length);
            
            return data;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Chunk {ChunkId} not found in S3", chunk.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve chunk {ChunkId} from S3", chunk.Id);
            return null;
        }
    }

    public async Task<bool> DeleteChunkAsync(FileChunk chunk)
    {
        try
        {
            var key = GetChunkKey(chunk);
            
            _logger.LogInformation("Deleting chunk {ChunkId} from S3 with key {Key}", chunk.Id, key);
            
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Successfully deleted chunk {ChunkId} from S3", chunk.Id);
                return true;
            }
            else
            {
                _logger.LogError("Failed to delete chunk {ChunkId} from S3. Status: {StatusCode}", 
                    chunk.Id, response.HttpStatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chunk {ChunkId} from S3", chunk.Id);
            return false;
        }
    }

    public Task<bool> ChunkExistsAsync(FileChunk chunk)
    {
        try
        {
            var key = GetChunkKey(chunk);
            
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            return _s3Client.GetObjectMetadataAsync(request)
                .ContinueWith(t => 
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception?.InnerException is AmazonS3Exception s3Ex && 
                            s3Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return false;
                        }
                        _logger.LogError(t.Exception, "Error checking if chunk {ChunkId} exists in S3", chunk.Id);
                        return false;
                    }
                    return t.Result.HttpStatusCode == System.Net.HttpStatusCode.OK;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if chunk {ChunkId} exists in S3", chunk.Id);
            return Task.FromResult(false);
        }
    }

    public async Task<long> GetChunkSizeAsync(FileChunk chunk)
    {
        try
        {
            var key = GetChunkKey(chunk);
            
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request);
            return response.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get size for chunk {ChunkId} from S3", chunk.Id);
            return 0;
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
        // For S3 storage, all chunks are stored in the same bucket
        // This method could be extended to distribute across multiple buckets
        return Task.FromResult(chunks);
    }

    public async Task<bool> ReplicateChunkAsync(FileChunk sourceChunk, StorageProvider targetProvider)
    {
        try
        {
            // Retrieve chunk data
            var chunkData = await RetrieveChunkAsync(sourceChunk);
            if (chunkData == null)
            {
                _logger.LogError("Cannot replicate chunk {ChunkId}: source data not found", sourceChunk.Id);
                return false;
            }

            // For now, we'll use the same S3 service for replication
            // In a real scenario, you might want to use a different S3 client for the target provider
            return await StoreChunkAsync(sourceChunk, chunkData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replicate chunk {ChunkId}", sourceChunk.Id);
            return false;
        }
    }

    private string GetChunkKey(FileChunk chunk)
    {
        return $"chunks/{chunk.StorageProviderId}/{chunk.FileId}/{chunk.Order:D6}.chunk";
    }

    public async Task<bool> TestProviderConnectionAsync(StorageProvider provider)
    {
        try
        {
            _logger.LogInformation("Testing S3 provider connection for {ProviderName}", provider.Name);
            
            // Test by listing buckets
            var response = await _s3Client.ListBucketsAsync();
            
            _logger.LogInformation("S3 provider connection test successful for {ProviderName}", provider.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 provider connection test failed for {ProviderName}", provider.Name);
            return false;
        }
    }

    public Task<long> GetAvailableSpaceAsync(StorageProvider provider)
    {
        try
        {
            // For S3, we can't easily get available space, so we'll return a large number
            // In a real scenario, you might want to implement quota checking
            _logger.LogInformation("Getting available space for S3 provider {ProviderName}", provider.Name);
            return Task.FromResult(long.MaxValue); // Unlimited for now
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available space for S3 provider {ProviderName}", provider.Name);
            return Task.FromResult(0L);
        }
    }

    public async Task<bool> IsProviderHealthyAsync(StorageProvider provider)
    {
        try
        {
            _logger.LogInformation("Checking health of S3 provider {ProviderName}", provider.Name);
            
            // Test by listing buckets
            var response = await _s3Client.ListBucketsAsync();
            
            var isHealthy = response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            _logger.LogInformation("S3 provider {ProviderName} health check: {IsHealthy}", provider.Name, isHealthy);
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for S3 provider {ProviderName}", provider.Name);
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            // Check if bucket exists by trying to get its location
            try
            {
                await _s3Client.GetBucketLocationAsync(_bucketName);
                _logger.LogInformation("S3 bucket {BucketName} already exists", _bucketName);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Creating S3 bucket: {BucketName}", _bucketName);
                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _bucketName
                });
                _logger.LogInformation("Successfully created S3 bucket: {BucketName}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket {BucketName} exists", _bucketName);
        }
    }
}
