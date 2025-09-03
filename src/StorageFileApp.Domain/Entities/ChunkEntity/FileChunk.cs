using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Domain.Entities.ChunkEntity;

public class FileChunk : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public Guid Id { get; private set; }
    public Guid FileId { get; private set; }
    public int Order { get; private set; }
    public long Size { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public Guid StorageProviderId { get; private set; }
    public ChunkStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
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
        
        // Add domain event
        _domainEvents.Add(new ChunkCreatedEvent(this));
    }
    
    public void UpdateStatus(ChunkStatus status)
    {
        var oldStatus = Status;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
        
        // Add domain event for status change
        if (oldStatus != status)
        {
            _domainEvents.Add(new ChunkStatusChangedEvent(this, oldStatus, status));
        }
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
