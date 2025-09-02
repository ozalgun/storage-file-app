using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.Interfaces;

public interface IStorageProviderRepository : IRepository<StorageProvider>
{
    Task<IEnumerable<StorageProvider>> GetActiveProvidersAsync();
    Task<IEnumerable<StorageProvider>> GetByTypeAsync(StorageProviderType type);
    Task<StorageProvider?> GetByNameAsync(string name);
    Task<IEnumerable<StorageProvider>> GetAvailableProvidersAsync();
    Task<bool> IsProviderAvailableAsync(Guid providerId);
    Task<int> GetActiveProviderCountAsync();
    Task<IEnumerable<StorageProvider>> GetProvidersByLoadAsync();
    Task<int> GetChunkCountByProviderIdAsync(Guid providerId);
}
