using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Entities.ChunkEntity;
using File = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public interface IFileMergingDomainService
{
    Task<byte[]> MergeChunksIntoFileAsync(IEnumerable<FileChunk> chunks, IEnumerable<byte[]> chunkData);
    Task<bool> ValidateMergedFileAsync(byte[] mergedData, File file);
    Task<long> CalculateMergedFileSizeAsync(IEnumerable<FileChunk> chunks);
    Task<bool> ValidateChunkSequenceForMergingAsync(IEnumerable<FileChunk> chunks);
    Task<byte[]> ExtractChunkDataAsync(FileChunk chunk, byte[] fullFileData);
}
