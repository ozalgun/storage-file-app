using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Events;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.ValueObjects;
using StorageFileApp.SharedKernel.Exceptions;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;
using DomainFileNotFoundException = StorageFileApp.SharedKernel.Exceptions.FileNotFoundException;

namespace StorageFileApp.Domain.Aggregates;

public class FileAggregate
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

        // Domain event'i ekle
        aggregate._domainEvents.Add(new FileCreatedEvent(file));

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

        // Domain event'i ekle
        _domainEvents.Add(new ChunkCreatedEvent(chunk));
    }

    private void UpdateFileStatus(FileStatus newStatus)
    {
        if (newStatus == File.Status)
            return;

        var oldStatus = File.Status;
        File.UpdateStatus(newStatus);

        // Domain event'i ekle
        _domainEvents.Add(new FileStatusChangedEvent(File, oldStatus, newStatus));
    }

    public void UpdateChunkStatus(int order, ChunkStatus newStatus)
    {
        var chunk = _chunks.FirstOrDefault(c => c.Order == order) ??
                    throw new InvalidFileOperationException("UpdateChunkStatus", $"Chunk with order {order} not found");
        var oldStatus = chunk.Status;
        chunk.UpdateStatus(newStatus);

        // Domain event'i ekle
        _domainEvents.Add(new ChunkStatusChangedEvent(chunk, oldStatus, newStatus));
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

        // Domain event'i ekle
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