using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Entities.ChunkEntity;

namespace StorageFileApp.Domain.Services;

public interface IStorageStrategyDomainService
{
    Task<StorageProvider> SelectOptimalProviderAsync(FileChunk chunk, IEnumerable<StorageProvider> providers);
    Task<bool> ValidateStorageProviderAsync(StorageProvider provider);
    Task<IEnumerable<StorageProvider>> GetFallbackProvidersAsync(StorageProvider primaryProvider, IEnumerable<StorageProvider> allProviders);
    Task<StorageProvider> SelectProviderByStrategyAsync(FileChunk chunk, IEnumerable<StorageProvider> providers, StorageStrategy strategy);
    Task<bool> IsProviderOverloadedAsync(StorageProvider provider);
}

public enum StorageStrategy
{
    RoundRobin,         // Sırayla dağıtım
    LoadBalanced,       // Yük dengeleme
    Geographic,         // Coğrafi yakınlık
    Performance,        // Performans bazlı
    CostOptimized,      // Maliyet optimizasyonu
    Redundancy          // Yedeklilik için
}
