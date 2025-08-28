namespace StorageFileApp.Domain.Entities.ChunkEntity;

public class FileChunk
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public int Order { get; set; }
    public long Size { get; set; }
    public required string Checksum { get; set; }
    public Guid StorageProviderId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    private FileChunk() { } 
    
    public FileChunk(Guid fileId, int order, long size, string checksum, Guid storageProviderId)
    {
        Id = Guid.NewGuid();
        FileId = fileId;
        Order = order;
        Size = size;
        Checksum = checksum ?? throw new ArgumentNullException(nameof(checksum));
        StorageProviderId = storageProviderId;
        CreatedAt = DateTime.UtcNow;
    }
}
