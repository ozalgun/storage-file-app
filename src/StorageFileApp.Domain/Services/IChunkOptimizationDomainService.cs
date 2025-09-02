using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;

namespace StorageFileApp.Domain.Services;

public interface IChunkOptimizationDomainService
{
    Task<ChunkOptimizationResult> OptimizeChunkSizeAsync(long fileSize, IEnumerable<StorageProvider> availableProviders);
    Task<bool> ShouldCompressChunkAsync(FileChunk chunk, byte[] chunkData);
    Task<CompressionStrategy> GetOptimalCompressionStrategyAsync(FileChunk chunk);
    Task<long> CalculateOptimalChunkCountAsync(long fileSize, long chunkSize);
    Task<ChunkDistributionPlan> CreateOptimalDistributionPlanAsync(IEnumerable<FileChunk> chunks, IEnumerable<StorageProvider> providers);
}

public class ChunkOptimizationResult
{
    public long OptimalChunkSize { get; set; }
    public int OptimalChunkCount { get; set; }
    public CompressionStrategy RecommendedCompression { get; set; }
    public TimeSpan EstimatedProcessingTime { get; set; }
    public long EstimatedStorageCost { get; set; }
}

public enum CompressionStrategy
{
    None,           // Sıkıştırma yok
    Gzip,          // Gzip sıkıştırma
    Deflate,       // Deflate sıkıştırma
    LZ4,           // LZ4 hızlı sıkıştırma
    Zstd           // Zstandard sıkıştırma
}

public class ChunkDistributionPlan
{
    public Dictionary<Guid, List<FileChunk>> ProviderChunkMapping { get; set; } = [];
    public Dictionary<Guid, long> ProviderLoadEstimate { get; set; } = [];
    public TimeSpan EstimatedCompletionTime { get; set; }
    public long TotalStorageRequired { get; set; }
}
