using StorageFileApp.Domain.Entities.ChunkEntity;
using System.Security.Cryptography;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public class FileIntegrityDomainService : IFileIntegrityDomainService
{
    public async Task<string> CalculateFileChecksumAsync(Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        return Convert.ToHexString(hashBytes);
    }
    
    public async Task<bool> ValidateFileIntegrityAsync(FileEntity file, IEnumerable<FileChunk> chunks, IEnumerable<byte[]> chunkData)
    {
        ArgumentNullException.ThrowIfNull(file);

        var chunksList = chunks.ToList();
        var chunkDataList = chunkData.ToList();
        
        if (chunksList.Count != chunkDataList.Count)
            return false;
            
        // Chunk sequence validation
        if (!await ValidateChunkSequenceAsync(chunksList))
            return false;
            
        // Total size validation
        var totalChunkSize = await CalculateTotalChunkSizeAsync(chunksList);
        if (totalChunkSize != file.Size)
            return false;
            
        // Individual chunk validation
        for (var i = 0; i < chunksList.Count; i++)
        {
            var chunk = chunksList[i];
            var data = chunkDataList[i];
            
            if (data.Length != chunk.Size)
                return false;
                
            // Checksum validation
            var calculatedChecksum = await CalculateChunkChecksumAsync(data);
            if (!string.Equals(calculatedChecksum, chunk.Checksum, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        
        return true;
    }
    
    public Task<bool> ValidateChunkSequenceAsync(IEnumerable<FileChunk> chunks)
    {
        var orderedChunks = chunks.OrderBy(c => c.Order).ToList();
        
        // Check if chunks are sequential starting from 0
        for (var i = 0; i < orderedChunks.Count; i++)
        {
            if (orderedChunks[i].Order != i)
                return Task.FromResult(false);
        }
        
        return Task.FromResult(true);
    }
    
    public Task<long> CalculateTotalChunkSizeAsync(IEnumerable<FileChunk> chunks)
    {
        var totalSize = chunks.Sum(c => c.Size);
        return Task.FromResult(totalSize);
    }
    
    private static async Task<string> CalculateChunkChecksumAsync(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(new MemoryStream(data));
        return Convert.ToHexString(hashBytes);
    }
}
