using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.SharedKernel.Exceptions;

namespace StorageFileApp.Domain.Services;

public class StorageStrategyDomainService : IStorageStrategyDomainService
{
    private readonly Dictionary<Guid, int> _providerLoadCount = new();
    private readonly object _loadLock = new object();
    
    public Task<StorageProvider> SelectOptimalProviderAsync(FileChunk chunk, IEnumerable<StorageProvider> providers)
    {
        var activeProviders = providers.Where(p => p.IsActive).ToList();
        
        if (!activeProviders.Any())
            throw new InvalidFileOperationException("SelectOptimalProvider", "No active storage providers available");
            
        // Default strategy: LoadBalanced
        return SelectProviderByStrategyAsync(chunk, activeProviders, StorageStrategy.LoadBalanced);
    }
    
    public Task<bool> ValidateStorageProviderAsync(StorageProvider provider)
    {
        if (provider == null)
            return Task.FromResult(false);
            
        // Basic validation
        if (!provider.IsActive)
            return Task.FromResult(false);
            
        if (string.IsNullOrWhiteSpace(provider.ConnectionString))
            return Task.FromResult(false);
            
        // Check if provider is overloaded
        var isOverloaded = IsProviderOverloadedAsync(provider).Result;
        if (isOverloaded)
            return Task.FromResult(false);
            
        return Task.FromResult(true);
    }
    
    public Task<IEnumerable<StorageProvider>> GetFallbackProvidersAsync(StorageProvider primaryProvider, IEnumerable<StorageProvider> allProviders)
    {
        var fallbackProviders = allProviders
            .Where(p => p.IsActive && p.Id != primaryProvider.Id)
            .OrderBy(p => p.Type) // Prioritize by type
            .Take(3); // Maximum 3 fallback providers
            
        return Task.FromResult<IEnumerable<StorageProvider>>(fallbackProviders);
    }
    
    public Task<StorageProvider> SelectProviderByStrategyAsync(FileChunk chunk, IEnumerable<StorageProvider> providers, StorageStrategy strategy)
    {
        var activeProviders = providers.Where(p => p.IsActive).ToList();
        
        return strategy switch
        {
            StorageStrategy.RoundRobin => SelectRoundRobinProviderAsync(activeProviders),
            StorageStrategy.LoadBalanced => SelectLoadBalancedProviderAsync(activeProviders),
            StorageStrategy.Geographic => SelectGeographicProviderAsync(activeProviders, chunk),
            StorageStrategy.Performance => SelectPerformanceBasedProviderAsync(activeProviders),
            StorageStrategy.CostOptimized => SelectCostOptimizedProviderAsync(activeProviders),
            StorageStrategy.Redundancy => SelectRedundancyProviderAsync(activeProviders),
            _ => SelectLoadBalancedProviderAsync(activeProviders)
        };
    }
    
    public Task<bool> IsProviderOverloadedAsync(StorageProvider provider)
    {
        lock (_loadLock)
        {
            var currentLoad = _providerLoadCount.GetValueOrDefault(provider.Id, 0);
            return Task.FromResult(currentLoad > 100); // Threshold: 100 concurrent operations
        }
    }
    
    private Task<StorageProvider> SelectRoundRobinProviderAsync(IList<StorageProvider> providers)
    {
        if (!providers.Any())
            throw new InvalidFileOperationException("SelectRoundRobinProvider", "No providers available");
            
        // Simple round-robin selection
        var index = DateTime.UtcNow.Ticks % providers.Count;
        return Task.FromResult(providers[(int)index]);
    }
    
    private Task<StorageProvider> SelectLoadBalancedProviderAsync(IList<StorageProvider> providers)
    {
        if (!providers.Any())
            throw new InvalidFileOperationException("SelectLoadBalancedProvider", "No providers available");
            
        // Select provider with lowest load
        StorageProvider? selectedProvider = null;
        var minLoad = int.MaxValue;
        
        lock (_loadLock)
        {
            foreach (var provider in providers)
            {
                var currentLoad = _providerLoadCount.GetValueOrDefault(provider.Id, 0);
                if (currentLoad < minLoad)
                {
                    minLoad = currentLoad;
                    selectedProvider = provider;
                }
            }
        }
        
        if (selectedProvider == null)
            throw new InvalidFileOperationException("SelectLoadBalancedProvider", "No suitable provider found");
            
        // Increment load count
        IncrementProviderLoad(selectedProvider.Id);
        
        return Task.FromResult(selectedProvider);
    }
    
    private Task<StorageProvider> SelectGeographicProviderAsync(IList<StorageProvider> providers, FileChunk chunk)
    {
        // For now, fallback to load balanced
        // In real implementation, this would consider geographic location
        return SelectLoadBalancedProviderAsync(providers);
    }
    
    private Task<StorageProvider> SelectPerformanceBasedProviderAsync(IList<StorageProvider> providers)
    {
        // Prioritize by provider type (assuming certain types are faster)
        var prioritizedProviders = providers
            .OrderBy(p => p.Type switch
            {
                StorageProviderType.FileSystem => 1,      // Fastest
                StorageProviderType.NetworkStorage => 2,   // Fast
                StorageProviderType.Database => 3,         // Medium
                StorageProviderType.CloudStorage => 4,     // Slower
                _ => 5
            })
            .ToList();
            
        return SelectLoadBalancedProviderAsync(prioritizedProviders);
    }
    
    private Task<StorageProvider> SelectCostOptimizedProviderAsync(IList<StorageProvider> providers)
    {
        // Prioritize by cost (assuming certain types are cheaper)
        var costOptimizedProviders = providers
            .OrderBy(p => p.Type switch
            {
                StorageProviderType.FileSystem => 1,      // Cheapest
                StorageProviderType.Database => 2,         // Cheap
                StorageProviderType.NetworkStorage => 3,   // Medium
                StorageProviderType.CloudStorage => 4,     // Expensive
                _ => 5
            })
            .ToList();
            
        return SelectLoadBalancedProviderAsync(costOptimizedProviders);
    }
    
    private Task<StorageProvider> SelectRedundancyProviderAsync(IList<StorageProvider> providers)
    {
        // Select provider that's different from the primary one
        // This would be used when we need to store a copy in a different provider
        var primaryProvider = providers.FirstOrDefault();
        if (primaryProvider == null)
            throw new InvalidFileOperationException("SelectRedundancyProvider", "No providers available");
            
        var redundancyProvider = providers
            .Where(p => p.Id != primaryProvider.Id && p.Type != primaryProvider.Type)
            .FirstOrDefault();
            
        return Task.FromResult(redundancyProvider ?? primaryProvider);
    }
    
    private void IncrementProviderLoad(Guid providerId)
    {
        lock (_loadLock)
        {
            _providerLoadCount[providerId] = _providerLoadCount.GetValueOrDefault(providerId, 0) + 1;
        }
    }
    
    public void DecrementProviderLoad(Guid providerId)
    {
        lock (_loadLock)
        {
            if (_providerLoadCount.ContainsKey(providerId))
            {
                _providerLoadCount[providerId] = Math.Max(0, _providerLoadCount[providerId] - 1);
            }
        }
    }
}
