using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.SharedKernel.Exceptions;
using File = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public class FileChunkingDomainService : IFileChunkingDomainService
{
    private const long MIN_CHUNK_SIZE = 64 * 1024; // 64KB
    private const long MAX_CHUNK_SIZE = 100 * 1024 * 1024; // 100MB
    private const int MAX_CHUNK_COUNT = 10000; // Maximum chunk sayısı
    
    public IEnumerable<ChunkInfo> CalculateOptimalChunks(long fileSize)
    {
        if (fileSize <= 0)
            throw new InvalidFileOperationException("CalculateOptimalChunks", "File size must be positive");
            
        var optimalChunkSize = CalculateOptimalChunkSize(fileSize);
        var chunkCount = (int)Math.Ceiling((double)fileSize / optimalChunkSize);
        
        if (chunkCount > MAX_CHUNK_COUNT)
            throw new InvalidFileOperationException("CalculateOptimalChunks", 
                $"File too large. Maximum {MAX_CHUNK_COUNT} chunks allowed.");
        
        return Enumerable.Range(0, chunkCount)
            .Select(i => 
            {
                var offset = i * optimalChunkSize;
                var size = Math.Min(optimalChunkSize, fileSize - offset);
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
        
        var chunks = new List<FileChunk>();
        var storageProviderIndex = 0;
        
        foreach (var chunkInfo in chunkInfos)
        {
            var storageProviderId = storageProviderIdsList[storageProviderIndex % storageProviderIdsList.Count];
            
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
    
    public bool ValidateChunkIntegrity(FileChunk chunk, byte[] chunkData)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));
            
        if (chunkData == null)
            throw new ArgumentNullException(nameof(chunkData));
            
        // Chunk size validation
        if (chunkData.Length != chunk.Size)
            return false;
            
        // Checksum validation (basit bir örnek)
        var calculatedChecksum = CalculateChecksum(chunkData);
        return string.Equals(calculatedChecksum, chunk.Checksum, StringComparison.OrdinalIgnoreCase);
    }
    
    public long CalculateOptimalChunkSize(long fileSize)
    {
        // Business rules for chunk sizing
        if (fileSize < 1024 * 1024) // < 1MB
            return MIN_CHUNK_SIZE; // 64KB chunks
            
        if (fileSize < 100 * 1024 * 1024) // < 100MB
            return 1024 * 1024; // 1MB chunks
            
        if (fileSize < 1024 * 1024 * 1024) // < 1GB
            return 10 * 1024 * 1024; // 10MB chunks
            
        return MAX_CHUNK_SIZE; // 100MB chunks
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
