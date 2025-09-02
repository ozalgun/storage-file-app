using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;

namespace StorageFileApp.Domain.Services;

public class ChunkOptimizationDomainService : IChunkOptimizationDomainService
{
    private const long MIN_CHUNK_SIZE = 64 * 1024; // 64KB
    private const long MAX_CHUNK_SIZE = 100 * 1024 * 1024; // 100MB
    private const int MAX_CHUNK_COUNT = 10000;
    
    public Task<ChunkOptimizationResult> OptimizeChunkSizeAsync(long fileSize, IEnumerable<StorageProvider> availableProviders)
    {
        var providers = availableProviders.Where(p => p.IsActive).ToList();
        
        var optimalChunkSize = CalculateOptimalChunkSize(fileSize);
        var optimalChunkCount = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
        
        var result = new ChunkOptimizationResult
        {
            OptimalChunkSize = optimalChunkSize,
            OptimalChunkCount = optimalChunkCount,
            RecommendedCompression = CompressionStrategy.None,
            EstimatedProcessingTime = TimeSpan.FromMinutes(1),
            EstimatedStorageCost = fileSize / (1024 * 1024) // Simple cost per MB
        };
        
        return Task.FromResult(result);
    }
    
    public Task<bool> ShouldCompressChunkAsync(FileChunk chunk, byte[] chunkData)
    {
        return Task.FromResult(false); // Simple implementation
    }
    
    public Task<CompressionStrategy> GetOptimalCompressionStrategyAsync(FileChunk chunk)
    {
        return Task.FromResult(CompressionStrategy.None); // Simple implementation
    }
    
    public Task<long> CalculateOptimalChunkCountAsync(long fileSize, long chunkSize)
    {
        if (fileSize <= 0 || chunkSize <= 0)
            return Task.FromResult(1L);
            
        var chunkCount = (long)Math.Ceiling((double)fileSize / chunkSize);
        return Task.FromResult(chunkCount);
    }
    
    public Task<ChunkDistributionPlan> CreateOptimalDistributionPlanAsync(IEnumerable<FileChunk> chunks, IEnumerable<StorageProvider> providers)
    {
        var plan = new ChunkDistributionPlan
        {
            TotalStorageRequired = chunks.Sum(c => c.Size),
            EstimatedCompletionTime = TimeSpan.FromMinutes(5)
        };
        
        return Task.FromResult(plan);
    }
    
    private long CalculateOptimalChunkSize(long fileSize)
    {
        return fileSize switch
        {
            < 1024 * 1024 => 64 * 1024,           // < 1MB: 64KB chunks
            < 100 * 1024 * 1024 => 1024 * 1024,   // < 100MB: 1MB chunks
            < 1024 * 1024 * 1024 => 10 * 1024 * 1024, // < 1GB: 10MB chunks
            _ => 100 * 1024 * 1024                 // >= 1GB: 100MB chunks
        };
    }
}
