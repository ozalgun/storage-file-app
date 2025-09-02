using Microsoft.EntityFrameworkCore;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Infrastructure.Data;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Infrastructure.Repositories;

public class FileRepository(StorageFileDbContext context) : BaseRepository<FileEntity>(context), IFileRepository
{
    public async Task<IEnumerable<FileEntity>> GetByStatusAsync(FileStatus status)
    {
        return await DbSet.Where(f => f.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<FileEntity>> GetByNameAsync(string name)
    {
        return await DbSet.Where(f => f.Name.Contains(name)).ToListAsync();
    }

    public async Task<IEnumerable<FileEntity>> GetBySizeRangeAsync(long minSize, long maxSize)
    {
        return await DbSet.Where(f => f.Size >= minSize && f.Size <= maxSize).ToListAsync();
    }

    public async Task<IEnumerable<FileEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await DbSet.Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate).ToListAsync();
    }

    public async Task<FileEntity?> GetByChecksumAsync(string checksum)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.Checksum == checksum);
    }

    public async Task<IEnumerable<FileEntity>> SearchAsync(string searchTerm)
    {
        return await DbSet.Where(f => f.Name.Contains(searchTerm) || 
                                      f.Metadata.Description != null && f.Metadata.Description.Contains(searchTerm))
                           .ToListAsync();
    }

    public async Task<(IEnumerable<FileEntity> Files, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, FileStatus? status = null, string? searchTerm = null)
    {
        var query = DbSet.AsQueryable();

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
}
