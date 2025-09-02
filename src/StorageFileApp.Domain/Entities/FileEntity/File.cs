using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.ValueObjects;

namespace StorageFileApp.Domain.Entities.FileEntity;

public class File
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public FileStatus Status { get; private set; }
    public FileMetadata Metadata { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    private File() { } 
    
    public File(string name, long size, string checksum, FileMetadata metadata)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Size = size;
        Checksum = checksum ?? throw new ArgumentNullException(nameof(checksum));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Status = FileStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateStatus(FileStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateMetadata(FileMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsAvailable()
    {
        Status = FileStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsFailed()
    {
        Status = FileStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}
