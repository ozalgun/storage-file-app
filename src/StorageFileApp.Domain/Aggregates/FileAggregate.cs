using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Events;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.ValueObjects;
using StorageFileApp.SharedKernel.Exceptions;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;
using DomainFileNotFoundException = StorageFileApp.SharedKernel.Exceptions.FileNotFoundException;

namespace StorageFileApp.Domain.Aggregates;

public class FileAggregate : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private FileEntity File { get; init; } = null!;
    public IReadOnlyCollection<FileChunk> Chunks => _chunks.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private readonly List<FileChunk> _chunks = [];

    private FileAggregate()
    {
    }

    public static FileAggregate Create(string name, long size, string checksum, FileMetadata metadata)
    {
        var file = new FileEntity(name, size, checksum, metadata);
        var aggregate = new FileAggregate { File = file };

        // File entity already adds FileCreatedEvent in its constructor
        return aggregate;
    }

    public void AddChunk(FileChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        if (chunk.FileId != File.Id)
            throw new InvalidFileOperationException("AddChunk", "Chunk does not belong to this file");

        if (_chunks.Any(c => c.Order == chunk.Order))
            throw new InvalidFileOperationException("AddChunk", $"Chunk with order {chunk.Order} already exists");

        _chunks.Add(chunk);
        _chunks.Sort((a, b) => a.Order.CompareTo(b.Order));

        // Chunk entity already adds ChunkCreatedEvent in its constructor
    }

    private void UpdateFileStatus(FileStatus newStatus)
    {
        if (newStatus == File.Status)
            return;

        File.UpdateStatus(newStatus);
        // File entity already adds FileStatusChangedEvent in UpdateStatus method
    }

    public void UpdateChunkStatus(int order, ChunkStatus newStatus)
    {
        var chunk = _chunks.FirstOrDefault(c => c.Order == order) ??
                    throw new InvalidFileOperationException("UpdateChunkStatus", $"Chunk with order {order} not found");
        chunk.UpdateStatus(newStatus);
        // Chunk entity already adds ChunkStatusChangedEvent in UpdateStatus method
    }

    private FileChunk GetChunk(int order)
    {
        var chunk = _chunks.FirstOrDefault(c => c.Order == order);
        if (chunk == null)
            throw new DomainFileNotFoundException(File.Id);

        return chunk;
    }

    private bool IsComplete()
    {
        return _chunks.Count > 0 && _chunks.All(c => c.Status == ChunkStatus.Stored);
    }

    public bool IsProcessing()
    {
        return File.Status == FileStatus.Processing ||
               _chunks.Any(c => c.Status == ChunkStatus.Storing);
    }

    public void MarkChunkAsStored(int order, Guid storageProviderId)
    {
        var chunk = GetChunk(order);
        chunk.UpdateStatus(ChunkStatus.Stored);

        // Add ChunkStoredEvent manually since it's not a simple status change
        _domainEvents.Add(new ChunkStoredEvent(chunk, storageProviderId));

        // Tüm chunk'lar stored ise file status'u güncelle
        if (IsComplete())
        {
            UpdateFileStatus(FileStatus.Stored);
        }
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public long GetTotalChunkSize()
    {
        return _chunks.Sum(c => c.Size);
    }

    public int GetChunkCount()
    {
        return _chunks.Count;
    }
}