using Microsoft.EntityFrameworkCore;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Infrastructure.Data;

namespace StorageFileApp.Infrastructure.Repositories;

public class StorageProviderRepository(StorageFileDbContext context)
    : BaseRepository<StorageProvider>(context), IStorageProviderRepository
{
    public async Task<IEnumerable<StorageProvider>> GetActiveProvidersAsync()
    {
        return await DbSet.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<StorageProvider>> GetByTypeAsync(StorageProviderType type)
    {
        return await DbSet.Where(p => p.Type == type).ToListAsync();
    }

    public async Task<StorageProvider?> GetByNameAsync(string name)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IEnumerable<StorageProvider>> GetAvailableProvidersAsync()
    {
        return await DbSet.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<bool> IsProviderAvailableAsync(Guid providerId)
    {
        return await DbSet.AnyAsync(p => p.Id == providerId && p.IsActive);
    }

    public async Task<int> GetActiveProviderCountAsync()
    {
        return await DbSet.CountAsync(p => p.IsActive);
    }

    public async Task<IEnumerable<StorageProvider>> GetProvidersByLoadAsync()
    {
        // This is a simplified implementation
        // In a real scenario, you might want to track load metrics
        return await DbSet.Where(p => p.IsActive)
                          .OrderBy(p => p.Type) // Simple ordering by type
                          .ToListAsync();
    }

    public async Task<int> GetChunkCountByProviderIdAsync(Guid providerId)
    {
        return await DbSet.CountAsync(c => c.Id == providerId); // Simplified for now
    }
}
