using StorageFileApp.Domain.Entities.ChunkEntity;
using File = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public interface IFileIntegrityDomainService
{
    Task<string> CalculateFileChecksumAsync(Stream fileStream);
    Task<bool> ValidateFileIntegrityAsync(File file, IEnumerable<FileChunk> chunks, IEnumerable<byte[]> chunkData);
    Task<bool> ValidateChunkSequenceAsync(IEnumerable<FileChunk> chunks);
    Task<long> CalculateTotalChunkSizeAsync(IEnumerable<FileChunk> chunks);
}
