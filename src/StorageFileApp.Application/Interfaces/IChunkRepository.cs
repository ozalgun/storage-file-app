using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.Interfaces;

public interface IChunkRepository : IRepository<FileChunk>
{
    Task<IEnumerable<FileChunk>> GetByFileIdAsync(Guid fileId);
    Task<IEnumerable<FileChunk>> GetByStorageProviderIdAsync(Guid storageProviderId);
    Task<IEnumerable<FileChunk>> GetByStatusAsync(ChunkStatus status);
    Task<IEnumerable<FileChunk>> GetByFileIdAndStatusAsync(Guid fileId, ChunkStatus status);
    Task<FileChunk?> GetByFileIdAndOrderAsync(Guid fileId, int order);
    Task<IEnumerable<FileChunk>> GetUnhealthyChunksAsync();
    Task<IEnumerable<FileChunk>> GetChunksNeedingReplicationAsync();
    Task<bool> AreAllChunksStoredAsync(Guid fileId);
    Task<int> GetChunkCountByFileIdAsync(Guid fileId);
    Task<long> GetTotalSizeByFileIdAsync(Guid fileId);
}
