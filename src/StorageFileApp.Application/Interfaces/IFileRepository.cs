using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Enums;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Application.Interfaces;

public interface IFileRepository : IRepository<FileEntity>
{
    Task<IEnumerable<FileEntity>> GetByStatusAsync(FileStatus status);
    Task<IEnumerable<FileEntity>> GetByNameAsync(string name);
    Task<IEnumerable<FileEntity>> GetBySizeRangeAsync(long minSize, long maxSize);
    Task<IEnumerable<FileEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<FileEntity?> GetByChecksumAsync(string checksum);
    Task<IEnumerable<FileEntity>> SearchAsync(string searchTerm);
    Task<(IEnumerable<FileEntity> Files, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, FileStatus? status = null, string? searchTerm = null);
}
