using Microsoft.EntityFrameworkCore;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Events;
using StorageFileApp.Infrastructure.Data;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Infrastructure.Repositories;

public class FileRepository(IDbContextFactory<StorageFileDbContext> contextFactory, IUnitOfWork unitOfWork, IDomainEventPublisher domainEventPublisher) : BaseRepository<FileEntity>(contextFactory, unitOfWork, domainEventPublisher), IFileRepository
{
    public async Task<IEnumerable<FileEntity>> GetByStatusAsync(FileStatus status)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileEntity>().Where(f => f.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<FileEntity>> GetByNameAsync(string name)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileEntity>().Where(f => f.Name.Contains(name)).ToListAsync();
    }

    public async Task<IEnumerable<FileEntity>> GetBySizeRangeAsync(long minSize, long maxSize)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileEntity>().Where(f => f.Size >= minSize && f.Size <= maxSize).ToListAsync();
    }

    public async Task<IEnumerable<FileEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileEntity>().Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate).ToListAsync();
    }

    public async Task<FileEntity?> GetByChecksumAsync(string checksum)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileEntity>().FirstOrDefaultAsync(f => f.Checksum == checksum);
    }

    public async Task<IEnumerable<FileEntity>> SearchAsync(string searchTerm)
    {
        using var context = await GetContextAsync();
        return await context.Set<FileEntity>().Where(f => f.Name.Contains(searchTerm) || 
                                      f.Metadata.Description != null && f.Metadata.Description.Contains(searchTerm))
                            .ToListAsync();
    }

    public async Task<(IEnumerable<FileEntity> Files, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, FileStatus? status = null, string? searchTerm = null)
    {
        using var context = await GetContextAsync();
        var query = context.Set<FileEntity>().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(f => f.Name.Contains(searchTerm) || 
                                   f.Metadata.Description != null && f.Metadata.Description.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (files, totalCount);
    }

    public async Task<int> GetCountAsync()
    {
        using var context = await GetContextAsync();
        return await context.Set<FileEntity>().CountAsync();
    }
}
