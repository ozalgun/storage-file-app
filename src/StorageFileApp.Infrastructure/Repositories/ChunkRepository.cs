using Microsoft.EntityFrameworkCore;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Events;
using StorageFileApp.Infrastructure.Data;

namespace StorageFileApp.Infrastructure.Repositories;

public class ChunkRepository(IDbContextFactory<StorageFileDbContext> contextFactory, IUnitOfWork unitOfWork, IDomainEventPublisher domainEventPublisher) : BaseRepository<FileChunk>(contextFactory, unitOfWork, domainEventPublisher), IChunkRepository
{
    public async Task<IEnumerable<FileChunk>> GetByFileIdAsync(Guid fileId)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().Where(c => c.FileId == fileId)
                          .OrderBy(c => c.Order)
                          .ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetByStorageProviderIdAsync(Guid storageProviderId)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().Where(c => c.StorageProviderId == storageProviderId).ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetByStatusAsync(ChunkStatus status)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().Where(c => c.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetByFileIdAndStatusAsync(Guid fileId, ChunkStatus status)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().Where(c => c.FileId == fileId && c.Status == status)
                          .OrderBy(c => c.Order)
                          .ToListAsync();
    }

    public async Task<FileChunk?> GetByFileIdAndOrderAsync(Guid fileId, int order)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().FirstOrDefaultAsync(c => c.FileId == fileId && c.Order == order);
    }

    public async Task<IEnumerable<FileChunk>> GetUnhealthyChunksAsync()
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().Where(c => c.Status == ChunkStatus.Error || c.Status == ChunkStatus.Deleted).ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetChunksNeedingReplicationAsync()
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().Where(c => c.Status == ChunkStatus.Error || c.Status == ChunkStatus.Deleted).ToListAsync();
    }

    public async Task<bool> AreAllChunksStoredAsync(Guid fileId)
    {
        using var context = await GetContextAsync();
        var totalChunks = await context.Set<FileChunk>().CountAsync(c => c.FileId == fileId);
        var storedChunks = await context.Set<FileChunk>().CountAsync(c => c.FileId == fileId && c.Status == ChunkStatus.Stored);
        
        return totalChunks > 0 && totalChunks == storedChunks;
    }

    public async Task<int> GetChunkCountByFileIdAsync(Guid fileId)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().CountAsync(c => c.FileId == fileId);
    }

    public async Task<long> GetTotalSizeByFileIdAsync(Guid fileId)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().Where(c => c.FileId == fileId).SumAsync(c => c.Size);
    }

    public async Task<int> GetCountAsync()
    {
        using var context = await GetContextAsync();
        return await context.Set<FileChunk>().CountAsync();
    }
}
