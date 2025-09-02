using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;

namespace StorageFileApp.Application.Interfaces;

public interface IStorageService
{
    Task<bool> StoreChunkAsync(FileChunk chunk, byte[] data);
    Task<byte[]?> RetrieveChunkAsync(FileChunk chunk);
    Task<bool> DeleteChunkAsync(FileChunk chunk);
    Task<bool> ChunkExistsAsync(FileChunk chunk);
    Task<long> GetChunkSizeAsync(FileChunk chunk);
    Task<bool> ValidateChunkIntegrityAsync(FileChunk chunk, byte[] data);
    Task<IEnumerable<FileChunk>> DistributeChunksAsync(IEnumerable<FileChunk> chunks);
    Task<bool> ReplicateChunkAsync(FileChunk sourceChunk, StorageProvider targetProvider);
    Task<bool> TestProviderConnectionAsync(StorageProvider provider);
    Task<long> GetAvailableSpaceAsync(StorageProvider provider);
    Task<bool> IsProviderHealthyAsync(StorageProvider provider);
}
