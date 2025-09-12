using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.SharedKernel.Exceptions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;

namespace StorageFileApp.Domain.Services;

public class FileStreamingDomainService : IFileStreamingDomainService
{
    private const int BufferSıze = 64 * 1024; // 64KB buffer
    private static readonly ConcurrentDictionary<Guid, StreamingProgress> ProgressTracker = new();
    
    public async Task<IEnumerable<ChunkProcessingResult>> ProcessFileStreamAsync(
        Stream fileStream, 
        long fileSize, 
        IEnumerable<StorageProvider> storageProviders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentNullException.ThrowIfNull(storageProviders);
        
        if (!fileStream.CanRead)
            throw new InvalidFileOperationException("ProcessFileStream", "Stream is not readable");
            
        var providers = storageProviders.Where(p => p.IsActive).ToList();
        if (!providers.Any())
            throw new InvalidFileOperationException("ProcessFileStream", "No active storage providers available");
        
        var fileId = Guid.NewGuid();
        var chunkingService = new FileChunkingDomainService();
        var chunkInfos = chunkingService.CalculateOptimalChunks(fileSize).ToList();
        
        // Initialize progress tracking
        var progress = new StreamingProgress
        {
            FileId = fileId,
            TotalChunks = chunkInfos.Count,
            TotalBytes = fileSize,
            ElapsedTime = TimeSpan.Zero
        };
        ProgressTracker[fileId] = progress;
        
        var stopwatch = Stopwatch.StartNew();
        var results = new List<ChunkProcessingResult>();
        
        try
        {
            // Process chunks sequentially to avoid stream position issues
            foreach (var (chunkInfo, index) in chunkInfos.Select((info, idx) => (info, idx)))
            {
                var result = await ProcessChunkAsync(
                    fileStream, 
                    chunkInfo, 
                    providers, 
                    fileId, 
                    index, 
                    progress, 
                    cancellationToken);
                
                results.Add(result);
            }
            
            stopwatch.Stop();
            progress.ElapsedTime = stopwatch.Elapsed;
        }
        finally
        {
            ProgressTracker.TryRemove(fileId, out _);
        }
        
        return results;
    }
    
    private async Task<ChunkProcessingResult> ProcessChunkAsync(
        Stream fileStream,
        ChunkInfo chunkInfo,
        List<StorageProvider> providers,
        Guid fileId,
        int index,
        StreamingProgress progress,
        CancellationToken cancellationToken)
    {
        var chunkStopwatch = Stopwatch.StartNew();
        var result = new ChunkProcessingResult
        {
            ChunkId = Guid.NewGuid(),
            Order = chunkInfo.Order,
            Size = chunkInfo.Size
        };
        
        try
        {
            // Read chunk data sequentially from stream
            var chunkData = new byte[chunkInfo.Size];
            var totalBytesRead = 0;
            
            while (totalBytesRead < chunkInfo.Size)
            {
                var bytesToRead = (int)Math.Min(chunkInfo.Size - totalBytesRead, BufferSıze);
                var bytesRead = await fileStream.ReadAsync(chunkData, totalBytesRead, bytesToRead, cancellationToken);
                
                if (bytesRead == 0)
                    throw new InvalidFileOperationException("ProcessChunk", "Unexpected end of stream while reading chunk");
                    
                totalBytesRead += bytesRead;
            }
            
            result.Size = chunkData.Length;
            
            // Calculate checksum
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(new MemoryStream(chunkData), cancellationToken);
            result.Checksum = Convert.ToHexString(hashBytes);
            
            // Select storage provider (round-robin)
            var provider = providers[index % providers.Count];
            result.StorageProviderId = provider.Id;
            
            // Simulate storage operation (in real implementation, this would call storage service)
            await Task.Delay(10, cancellationToken); // Simulate I/O delay
            
            result.IsSuccess = true;
            
            // Update progress
            progress.IncrementProcessedChunks();
            progress.IncrementSuccessfulChunks();
            progress.AddProcessedBytes(chunkData.Length);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            progress.IncrementFailedChunks();
        }
        finally
        {
            chunkStopwatch.Stop();
            result.ProcessingTime = chunkStopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<byte[]> ExtractChunkDataAsync(
        Stream fileStream, 
        long offset, 
        long size, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
            
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive");
        
        // Seek to the offset
        if (fileStream.CanSeek)
        {
            fileStream.Position = offset;
        }
        else
        {
            // For non-seekable streams, we need to read from the beginning
            var buffer = new byte[offset];
            var totalRead = 0;
            while (totalRead < offset)
            {
                var bytesRead = await fileStream.ReadAsync(buffer, totalRead, (int)Math.Min(offset - totalRead, BufferSıze), cancellationToken);
                if (bytesRead == 0)
                    throw new InvalidFileOperationException("ExtractChunkData", "Unexpected end of stream");
                totalRead += bytesRead;
            }
        }
        
        // Read the chunk data
        var chunkData = new byte[size];
        var totalBytesRead = 0;
        
        while (totalBytesRead < size)
        {
            var bytesToRead = (int)Math.Min(size - totalBytesRead, BufferSıze);
            var bytesRead = await fileStream.ReadAsync(chunkData, totalBytesRead, bytesToRead, cancellationToken);
            
            if (bytesRead == 0)
                throw new InvalidFileOperationException("ExtractChunkData", "Unexpected end of stream while reading chunk");
                
            totalBytesRead += bytesRead;
        }
        
        return chunkData;
    }
    
    public async Task<bool> ValidateChunkDataAsync(
        byte[] chunkData, 
        string expectedChecksum, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunkData);
        
        if (string.IsNullOrWhiteSpace(expectedChecksum))
            return false;
        
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(new MemoryStream(chunkData), cancellationToken);
        var calculatedChecksum = Convert.ToHexString(hashBytes);
        
        return string.Equals(calculatedChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
    
    public Task<StreamingProgress> GetStreamingProgressAsync(
        Guid fileId, 
        CancellationToken cancellationToken = default)
    {
        if (ProgressTracker.TryGetValue(fileId, out var progress))
        {
            // Calculate estimated remaining time
            if (progress.ProcessedChunks > 0 && progress.ElapsedTime.TotalSeconds > 0)
            {
                var averageTimePerChunk = progress.ElapsedTime.TotalSeconds / progress.ProcessedChunks;
                var remainingChunks = progress.TotalChunks - progress.ProcessedChunks;
                progress.EstimatedRemainingTime = TimeSpan.FromSeconds(averageTimePerChunk * remainingChunks);
            }
        }
        
        return Task.FromResult(progress ?? new StreamingProgress { FileId = fileId });
    }
}
