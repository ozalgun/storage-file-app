using Microsoft.EntityFrameworkCore;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Infrastructure.Data;

namespace StorageFileApp.Infrastructure.Repositories;

public class ChunkRepository(StorageFileDbContext context) : BaseRepository<FileChunk>(context), IChunkRepository
{
    public async Task<IEnumerable<FileChunk>> GetByFileIdAsync(Guid fileId)
    {
        return await DbSet.Where(c => c.FileId == fileId)
                          .OrderBy(c => c.Order)
                          .ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetByStorageProviderIdAsync(Guid storageProviderId)
    {
        return await DbSet.Where(c => c.StorageProviderId == storageProviderId).ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetByStatusAsync(ChunkStatus status)
    {
        return await DbSet.Where(c => c.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetByFileIdAndStatusAsync(Guid fileId, ChunkStatus status)
    {
        return await DbSet.Where(c => c.FileId == fileId && c.Status == status)
                          .OrderBy(c => c.Order)
                          .ToListAsync();
    }

    public async Task<FileChunk?> GetByFileIdAndOrderAsync(Guid fileId, int order)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.FileId == fileId && c.Order == order);
    }

    public async Task<IEnumerable<FileChunk>> GetUnhealthyChunksAsync()
    {
        return await DbSet.Where(c => c.Status == ChunkStatus.Error || c.Status == ChunkStatus.Deleted).ToListAsync();
    }

    public async Task<IEnumerable<FileChunk>> GetChunksNeedingReplicationAsync()
    {
        return await DbSet.Where(c => c.Status == ChunkStatus.Error || c.Status == ChunkStatus.Deleted).ToListAsync();
    }

    public async Task<bool> AreAllChunksStoredAsync(Guid fileId)
    {
        var totalChunks = await DbSet.CountAsync(c => c.FileId == fileId);
        var storedChunks = await DbSet.CountAsync(c => c.FileId == fileId && c.Status == ChunkStatus.Stored);
        
        return totalChunks > 0 && totalChunks == storedChunks;
    }

    public async Task<int> GetChunkCountByFileIdAsync(Guid fileId)
    {
        return await DbSet.CountAsync(c => c.FileId == fileId);
    }

    public async Task<long> GetTotalSizeByFileIdAsync(Guid fileId)
    {
        return await DbSet.Where(c => c.FileId == fileId).SumAsync(c => c.Size);
    }
}
