using Microsoft.EntityFrameworkCore;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Events;
using StorageFileApp.Domain.Constants;
using StorageFileApp.Infrastructure.Data;

namespace StorageFileApp.Infrastructure.Repositories;

public class StorageProviderRepository(IDbContextFactory<StorageFileDbContext> contextFactory, IUnitOfWork unitOfWork, IDomainEventPublisher domainEventPublisher)
    : BaseRepository<StorageProvider>(contextFactory, unitOfWork, domainEventPublisher), IStorageProviderRepository
{
    public async Task<IEnumerable<StorageProvider>> GetActiveProvidersAsync()
    {
        using var context = await GetContextAsync();
        var providers = await context.Set<StorageProvider>().Where(p => p.IsActive).ToListAsync();
        
        // Validate provider count limits
        if (providers.Count() > DomainConstants.MAX_STORAGE_PROVIDERS)
        {
            throw new InvalidOperationException($"Too many active storage providers. Maximum allowed: {DomainConstants.MAX_STORAGE_PROVIDERS}");
        }
        
        if (providers.Count() < DomainConstants.MIN_STORAGE_PROVIDERS)
        {
            throw new InvalidOperationException($"Insufficient storage providers. Minimum required: {DomainConstants.MIN_STORAGE_PROVIDERS}");
        }
        
        return providers;
    }

    public async Task<IEnumerable<StorageProvider>> GetByTypeAsync(StorageProviderType type)
    {
        using var context = await GetContextAsync();
        return await context.Set<StorageProvider>().Where(p => p.Type == type).ToListAsync();
    }

    public async Task<StorageProvider?> GetByNameAsync(string name)
    {
        using var context = await GetContextAsync();
        return await context.Set<StorageProvider>().FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IEnumerable<StorageProvider>> GetAvailableProvidersAsync()
    {
        using var context = await GetContextAsync();
        var providers = await context.Set<StorageProvider>().Where(p => p.IsActive).ToListAsync();
        
        // Shuffle providers to ensure varied round-robin distribution
        // This prevents database order from affecting chunk distribution
        var random = new Random();
        return providers.OrderBy(x => random.Next()).ToList();
    }

    public async Task<bool> IsProviderAvailableAsync(Guid providerId)
    {
        using var context = await GetContextAsync();
        return await context.Set<StorageProvider>().AnyAsync(p => p.Id == providerId && p.IsActive);
    }

    public async Task<int> GetActiveProviderCountAsync()
    {
        using var context = await GetContextAsync();
        return await context.Set<StorageProvider>().CountAsync(p => p.IsActive);
    }

    public async Task<IEnumerable<StorageProvider>> GetProvidersByLoadAsync()
    {
        using var context = await GetContextAsync();
        // This is a simplified implementation
        // In a real scenario, you might want to track load metrics
        return await context.Set<StorageProvider>().Where(p => p.IsActive)
                          .OrderBy(p => p.Type) // Simple ordering by type
                          .ToListAsync();
    }

    public async Task<int> GetChunkCountByProviderIdAsync(Guid providerId)
    {
        using var context = await GetContextAsync();
        return await context.Set<StorageProvider>().CountAsync(c => c.Id == providerId); // Simplified for now
    }
}
