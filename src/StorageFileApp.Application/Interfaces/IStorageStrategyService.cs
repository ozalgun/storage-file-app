using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;

namespace StorageFileApp.Application.Interfaces;

public interface IStorageStrategyService
{
    Task<StorageProvider> SelectStorageProviderAsync(FileChunk chunk, IEnumerable<StorageProvider> availableProviders);
    Task<IEnumerable<StorageProvider>> GetStorageProvidersForFileAsync(long fileSize, IEnumerable<StorageProvider> availableProviders);
}
