using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Entities.ChunkEntity;
using File = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public interface IFileChunkingDomainService
{
    IEnumerable<ChunkInfo> CalculateOptimalChunks(long fileSize);
    IEnumerable<FileChunk> CreateChunks(File file, IEnumerable<ChunkInfo> chunkInfos, IEnumerable<Guid> storageProviderIds);
    bool ValidateChunkIntegrity(FileChunk chunk, byte[] chunkData);
    long CalculateOptimalChunkSize(long fileSize);
}

public record ChunkInfo(int Order, long Size, long Offset, string Checksum);
