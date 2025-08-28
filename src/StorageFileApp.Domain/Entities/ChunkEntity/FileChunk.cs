using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Domain.Entities.ChunkEntity;

public class FileChunk
{
    public Guid Id { get; private set; }
    public Guid FileId { get; private set; }
    public int Order { get; private set; }
    public long Size { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public Guid StorageProviderId { get; private set; }
    public ChunkStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    private FileChunk() { } 
    
    public FileChunk(Guid fileId, int order, long size, string checksum, Guid storageProviderId)
    {
        Id = Guid.NewGuid();
        FileId = fileId;
        Order = order;
        Size = size;
        Checksum = checksum ?? throw new ArgumentNullException(nameof(checksum));
        StorageProviderId = storageProviderId;
        Status = ChunkStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateStatus(ChunkStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
