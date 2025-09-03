using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Infrastructure.Services;

public class StorageStrategyService : IStorageStrategyService
{
    private readonly ILogger<StorageStrategyService> _logger;
    private readonly IConfiguration _configuration;

    public StorageStrategyService(
        ILogger<StorageStrategyService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<StorageProvider> SelectStorageProviderAsync(FileChunk chunk, IEnumerable<StorageProvider> availableProviders)
    {
        _logger.LogInformation("Selecting storage provider for chunk {ChunkId} (Size: {ChunkSize} bytes)", 
            chunk.Id, chunk.Size);

        var strategy = _configuration["StorageSettings:Strategy"] ?? "RoundRobin";
        
        return strategy switch
        {
            "RoundRobin" => SelectRoundRobin(chunk, availableProviders),
            "FileSizeBased" => SelectByFileSize(chunk, availableProviders),
            "LoadBalanced" => await SelectByLoadBalance(chunk, availableProviders),
            "Random" => SelectRandom(chunk, availableProviders),
            _ => SelectRoundRobin(chunk, availableProviders)
        };
    }

    public Task<IEnumerable<StorageProvider>> GetStorageProvidersForFileAsync(long fileSize, IEnumerable<StorageProvider> availableProviders)
    {
        _logger.LogInformation("Getting storage providers for file (Size: {FileSize} bytes)", fileSize);

        var strategy = _configuration["StorageSettings:Strategy"] ?? "RoundRobin";
        var providers = availableProviders.ToList();

        // File size-based provider selection
        if (fileSize > long.Parse(_configuration["StorageSettings:MinIOThresholdBytes"] ?? "104857600")) // 100MB
        {
            _logger.LogInformation("Large file detected, preferring MinIO providers");
            return Task.FromResult<IEnumerable<StorageProvider>>(providers.Where(p => p.Type == StorageProviderType.MinIO).ToList());
        }

        // Small files can use any provider
        return Task.FromResult<IEnumerable<StorageProvider>>(providers);
    }

    private StorageProvider SelectRoundRobin(FileChunk chunk, IEnumerable<StorageProvider> availableProviders)
    {
        var providers = availableProviders.ToList();
        var index = (int)(chunk.Order % providers.Count);
        var selectedProvider = providers[index];
        
        _logger.LogInformation("Round-robin selection: Chunk {ChunkOrder} -> Provider {ProviderName}", 
            chunk.Order, selectedProvider.Name);
        
        return selectedProvider;
    }

    private StorageProvider SelectByFileSize(FileChunk chunk, IEnumerable<StorageProvider> availableProviders)
    {
        var providers = availableProviders.ToList();
        
        // Large chunks go to MinIO, small chunks to FileSystem
        if (chunk.Size > long.Parse(_configuration["StorageSettings:ChunkMinIOThresholdBytes"] ?? "10485760")) // 10MB
        {
            var minioProvider = providers.FirstOrDefault(p => p.Type == StorageProviderType.MinIO);
            if (minioProvider != null)
            {
                _logger.LogInformation("Large chunk ({ChunkSize} bytes) -> MinIO Provider {ProviderName}", 
                    chunk.Size, minioProvider.Name);
                return minioProvider;
            }
        }

        // Default to FileSystem for small chunks
        var fileSystemProvider = providers.FirstOrDefault(p => p.Type == StorageProviderType.FileSystem);
        if (fileSystemProvider != null)
        {
            _logger.LogInformation("Small chunk ({ChunkSize} bytes) -> FileSystem Provider {ProviderName}", 
                chunk.Size, fileSystemProvider.Name);
            return fileSystemProvider;
        }

        // Fallback to first available provider
        return providers.First();
    }

    private async Task<StorageProvider> SelectByLoadBalance(FileChunk chunk, IEnumerable<StorageProvider> availableProviders)
    {
        var providers = availableProviders.ToList();
        var providerLoads = new Dictionary<StorageProvider, long>();

        // Get current load for each provider (simplified - in real scenario, you'd query actual metrics)
        foreach (var provider in providers)
        {
            // This is a simplified load calculation
            // In a real scenario, you'd query actual storage usage, I/O metrics, etc.
            var load = await GetProviderLoadAsync(provider);
            providerLoads[provider] = load;
        }

        // Select provider with lowest load
        var selectedProvider = providerLoads.OrderBy(kvp => kvp.Value).First().Key;
        
        _logger.LogInformation("Load-balanced selection: Chunk {ChunkId} -> Provider {ProviderName} (Load: {Load})", 
            chunk.Id, selectedProvider.Name, providerLoads[selectedProvider]);
        
        return selectedProvider;
    }

    private StorageProvider SelectRandom(FileChunk chunk, IEnumerable<StorageProvider> availableProviders)
    {
        var providers = availableProviders.ToList();
        var random = new Random();
        var selectedProvider = providers[random.Next(providers.Count)];
        
        _logger.LogInformation("Random selection: Chunk {ChunkId} -> Provider {ProviderName}", 
            chunk.Id, selectedProvider.Name);
        
        return selectedProvider;
    }

    private async Task<long> GetProviderLoadAsync(StorageProvider provider)
    {
        // Simplified load calculation
        // In a real scenario, you'd query actual metrics from the storage provider
        await Task.Delay(1); // Simulate async operation
        
        return provider.Type switch
        {
            StorageProviderType.FileSystem => new Random().Next(1000, 5000), // Simulated load
            StorageProviderType.MinIO => new Random().Next(500, 2000), // MinIO typically has lower load
            _ => 1000
        };
    }
}
