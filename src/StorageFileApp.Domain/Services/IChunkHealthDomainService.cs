using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;

namespace StorageFileApp.Domain.Services;

public interface IChunkHealthDomainService
{
    Task<bool> ValidateChunkHealthAsync(FileChunk chunk);
    Task<IEnumerable<FileChunk>> GetUnhealthyChunksAsync(IEnumerable<FileChunk> chunks);
    Task<bool> IsChunkReplicationNeededAsync(FileChunk chunk);
    Task<ChunkHealthStatus> GetChunkHealthStatusAsync(FileChunk chunk);
    Task<bool> ShouldReplicateChunkAsync(FileChunk chunk, IEnumerable<StorageProvider> availableProviders);
    Task<IEnumerable<StorageProvider>> GetReplicationTargetsAsync(FileChunk chunk, IEnumerable<StorageProvider> providers);
}

public enum ChunkHealthStatus
{
    Healthy,        // Chunk sağlıklı
    Degraded,      // Chunk hasarlı ama okunabilir
    Corrupted,     // Chunk bozuk
    Missing,       // Chunk eksik
    Replicating,   // Chunk replication sürecinde
    Unknown        // Durum bilinmiyor
}
