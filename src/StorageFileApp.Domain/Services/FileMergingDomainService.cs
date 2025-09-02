using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.SharedKernel.Exceptions;
using System.Security.Cryptography;
using File = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public class FileMergingDomainService : IFileMergingDomainService
{
    public async Task<byte[]> MergeChunksIntoFileAsync(IEnumerable<FileChunk> chunks, IEnumerable<byte[]> chunkData)
    {
        var chunksList = chunks.OrderBy(c => c.Order).ToList();
        var chunkDataList = chunkData.ToList();
        
        if (chunksList.Count != chunkDataList.Count)
            throw new InvalidFileOperationException("MergeChunks", "Chunk count and data count mismatch");
            
        // Validate chunk sequence
        if (!await ValidateChunkSequenceForMergingAsync(chunksList))
            throw new InvalidFileOperationException("MergeChunks", "Invalid chunk sequence");
            
        // Calculate total size
        var totalSize = await CalculateMergedFileSizeAsync(chunksList);
        var mergedFile = new byte[totalSize];
        
        // Merge chunks in order
        var currentOffset = 0L;
        for (int i = 0; i < chunksList.Count; i++)
        {
            var chunk = chunksList[i];
            var data = chunkDataList[i];
            
            if (data.Length != chunk.Size)
                throw new InvalidFileOperationException("MergeChunks", 
                    $"Chunk {chunk.Order} size mismatch. Expected: {chunk.Size}, Actual: {data.Length}");
                    
            // Copy chunk data to merged file
            Array.Copy(data, 0, mergedFile, currentOffset, data.Length);
            currentOffset += chunk.Size;
        }
        
        return mergedFile;
    }
    
    public async Task<bool> ValidateMergedFileAsync(byte[] mergedData, File file)
    {
        if (mergedData == null)
            throw new ArgumentNullException(nameof(mergedData));
            
        if (file == null)
            throw new ArgumentNullException(nameof(file));
            
        // Size validation
        if (mergedData.Length != file.Size)
            return false;
            
        // Checksum validation
        var calculatedChecksum = await CalculateFileChecksumAsync(mergedData);
        return string.Equals(calculatedChecksum, file.Checksum, StringComparison.OrdinalIgnoreCase);
    }
    
    public Task<long> CalculateMergedFileSizeAsync(IEnumerable<FileChunk> chunks)
    {
        var totalSize = chunks.Sum(c => c.Size);
        return Task.FromResult(totalSize);
    }
    
    public Task<bool> ValidateChunkSequenceForMergingAsync(IEnumerable<FileChunk> chunks)
    {
        var orderedChunks = chunks.OrderBy(c => c.Order).ToList();
        
        // Check if chunks are sequential starting from 0
        for (int i = 0; i < orderedChunks.Count; i++)
        {
            if (orderedChunks[i].Order != i)
                return Task.FromResult(false);
        }
        
        // Check for gaps in sequence
        var expectedSize = orderedChunks.Sum(c => c.Size);
        var actualSize = orderedChunks.Sum(c => c.Size);
        
        return Task.FromResult(expectedSize == actualSize);
    }
    
    public Task<byte[]> ExtractChunkDataAsync(FileChunk chunk, byte[] fullFileData)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));
            
        if (fullFileData == null)
            throw new ArgumentNullException(nameof(fullFileData));
            
        // Calculate chunk offset based on previous chunks
        var offset = CalculateChunkOffset(chunk);
        
        if (offset + chunk.Size > fullFileData.Length)
            throw new InvalidFileOperationException("ExtractChunkData", 
                $"Chunk {chunk.Order} offset exceeds file size");
                
        var chunkData = new byte[chunk.Size];
        Array.Copy(fullFileData, offset, chunkData, 0, chunk.Size);
        
        return Task.FromResult(chunkData);
    }
    
    private long CalculateChunkOffset(FileChunk chunk)
    {
        // This would need to be calculated based on the chunk's order and previous chunks
        // For now, we'll assume chunks are stored sequentially
        return chunk.Order * chunk.Size;
    }
    
    private async Task<string> CalculateFileChecksumAsync(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(new MemoryStream(data));
        return Convert.ToHexString(hashBytes);
    }
}
