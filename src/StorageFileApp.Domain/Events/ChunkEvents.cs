using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Domain.Events;

public class ChunkCreatedEvent(FileChunk chunk) : BaseDomainEvent
{
    public FileChunk Chunk { get; } = chunk ?? throw new ArgumentNullException(nameof(chunk));
}

public class ChunkStatusChangedEvent(FileChunk chunk, ChunkStatus oldStatus, ChunkStatus newStatus)
    : BaseDomainEvent
{
    public FileChunk Chunk { get; } = chunk ?? throw new ArgumentNullException(nameof(chunk));
    public ChunkStatus OldStatus { get; } = oldStatus;
    public ChunkStatus NewStatus { get; } = newStatus;
}

public class ChunkStoredEvent(FileChunk chunk, Guid storageProviderId) : BaseDomainEvent
{
    public FileChunk Chunk { get; } = chunk ?? throw new ArgumentNullException(nameof(chunk));
    public Guid StorageProviderId { get; } = storageProviderId;
}
