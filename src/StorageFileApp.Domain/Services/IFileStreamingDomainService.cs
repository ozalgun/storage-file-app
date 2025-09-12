using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;

namespace StorageFileApp.Domain.Services;

public interface IFileStreamingDomainService
{
    Task<IEnumerable<ChunkProcessingResult>> ProcessFileStreamAsync(
        Stream fileStream, 
        long fileSize, 
        IEnumerable<StorageProvider> storageProviders,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExtractChunkDataAsync(
        Stream fileStream, 
        long offset, 
        long size, 
        CancellationToken cancellationToken = default);
    
    Task<bool> ValidateChunkDataAsync(
        byte[] chunkData, 
        string expectedChecksum, 
        CancellationToken cancellationToken = default);
    
    Task<StreamingProgress> GetStreamingProgressAsync(
        Guid fileId, 
        CancellationToken cancellationToken = default);
}

public class ChunkProcessingResult
{
    public Guid ChunkId { get; set; }
    public int Order { get; set; }
    public long Size { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public Guid StorageProviderId { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public byte[]? ChunkData { get; set; }
}

public class StreamingProgress
{
    public Guid FileId { get; set; }
    public int TotalChunks { get; set; }
    public int ProcessedChunks { get; set; }
    public int SuccessfulChunks { get; set; }
    public int FailedChunks { get; set; }
    public double ProgressPercentage => TotalChunks > 0 ? (double)ProcessedChunks / TotalChunks * 100 : 0;
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedRemainingTime { get; set; }
    public long ProcessedBytes { get; set; }
    public long TotalBytes { get; set; }
    
    public void IncrementProcessedChunks() => ProcessedChunks++;
    public void IncrementSuccessfulChunks() => SuccessfulChunks++;
    public void IncrementFailedChunks() => FailedChunks++;
    public void AddProcessedBytes(long bytes) => ProcessedBytes += bytes;
}
