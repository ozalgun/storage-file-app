using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Constants;
using StorageFileApp.SharedKernel.Exceptions;
using File = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public class FileChunkingDomainService : IFileChunkingDomainService
{
    
    public IEnumerable<ChunkInfo> CalculateOptimalChunks(long fileSize)
    {
        if (fileSize <= 0)
            throw new InvalidFileOperationException("CalculateOptimalChunks", "File size must be positive");
            
        var optimalChunkSize = CalculateOptimalChunkSize(fileSize);
        var chunkCount = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
        
        if (chunkCount > DomainConstants.MAX_CHUNK_COUNT)
            throw new InvalidFileOperationException("CalculateOptimalChunks", 
                $"File too large. Maximum {DomainConstants.MAX_CHUNK_COUNT} chunks allowed.");
        
        return Enumerable.Range(0, chunkCount)
            .Select(i => 
            {
                var offset = i * optimalChunkSize;
                var remainingBytes = fileSize - offset;
                var size = Math.Min(optimalChunkSize, remainingBytes);
                
                // Son chunk için size kontrolü
                if (size <= 0)
                    throw new InvalidFileOperationException("CalculateOptimalChunks", 
                        $"Invalid chunk size calculated for chunk {i}. Offset: {offset}, FileSize: {fileSize}");
                
                return new ChunkInfo(i, size, offset, string.Empty); // Checksum sonra hesaplanacak
            });
    }
    
    public IEnumerable<FileChunk> CreateChunks(File file, IEnumerable<ChunkInfo> chunkInfos, IEnumerable<Guid> storageProviderIds)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
            
        var storageProviderIdsList = storageProviderIds.ToList();
        if (!storageProviderIdsList.Any())
            throw new InvalidFileOperationException("CreateChunks", "At least one storage provider is required");
        
        // Shuffle provider IDs to ensure true round-robin distribution
        // This prevents the same provider order from database affecting distribution
        // Use file ID as seed for deterministic but varied distribution
        var random = new Random(file.Id.GetHashCode());
        var shuffledProviderIds = storageProviderIdsList.OrderBy(x => random.Next()).ToList();
        
        var chunks = new List<FileChunk>();
        var storageProviderIndex = 0;
        
        foreach (var chunkInfo in chunkInfos)
        {
            var storageProviderId = shuffledProviderIds[storageProviderIndex % shuffledProviderIds.Count];
            
            var chunk = new FileChunk(
                file.Id, 
                chunkInfo.Order, 
                chunkInfo.Size, 
                chunkInfo.Checksum, 
                storageProviderId
            );
            
            chunks.Add(chunk);
            storageProviderIndex++;
        }
        
        return chunks;
    }
    
    
    public long CalculateOptimalChunkSize(long fileSize)
    {
        // Business rules for chunk sizing
        if (fileSize <= 1024 * 1024) // <= 1MB
            return DomainConstants.MIN_CHUNK_SIZE; // 64KB chunks
            
        if (fileSize < 100 * 1024 * 1024) // < 100MB
            return DomainConstants.DEFAULT_CHUNK_SIZE; // 1MB chunks
            
        if (fileSize < 1024 * 1024 * 1024) // < 1GB
            return 10 * 1024 * 1024; // 10MB chunks
            
        return DomainConstants.MAX_CHUNK_SIZE; // 100MB chunks
    }
    
    private string CalculateChecksum(byte[] data)
    {
        // Basit bir checksum hesaplama (gerçek uygulamada SHA256 kullanılabilir)
        var hash = 0;
        foreach (var b in data)
        {
            hash = ((hash << 5) + hash) ^ b;
        }
        return Math.Abs(hash).ToString("X8");
    }
}
