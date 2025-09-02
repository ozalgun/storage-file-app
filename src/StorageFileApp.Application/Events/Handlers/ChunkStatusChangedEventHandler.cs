using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Events;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.Events.Handlers;

public class ChunkStatusChangedEventHandler(
    ILogger<ChunkStatusChangedEventHandler> logger,
    IFileRepository fileRepository)
    : IDomainEventHandler<ChunkStatusChangedEvent>
{
    private readonly ILogger<ChunkStatusChangedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));

    public async Task HandleAsync(ChunkStatusChangedEvent @event)
    {
        _logger.LogInformation("Handling ChunkStatusChangedEvent for chunk {ChunkId}: {OldStatus} -> {NewStatus}", 
            @event.Chunk.Id, @event.OldStatus, @event.NewStatus);

        try
        {
            // Handle different chunk status transitions
            switch (@event.NewStatus)
            {
                case ChunkStatus.Stored:
                    await HandleStoredStatusAsync(@event);
                    break;
                case ChunkStatus.Error:
                    await HandleErrorStatusAsync(@event);
                    break;
                case ChunkStatus.Deleted:
                    await HandleDeletedStatusAsync(@event);
                    break;
                default:
                    _logger.LogDebug("Chunk {ChunkId} status changed to {NewStatus}", @event.Chunk.Id, @event.NewStatus);
                    break;
            }

            _logger.LogInformation("Successfully processed ChunkStatusChangedEvent for chunk {ChunkId}", @event.Chunk.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ChunkStatusChangedEvent for chunk {ChunkId}", @event.Chunk.Id);
            throw;
        }
    }

    private async Task HandleStoredStatusAsync(ChunkStatusChangedEvent @event)
    {
        _logger.LogInformation("Chunk {ChunkId} successfully stored - checking file completion status", @event.Chunk.Id);
        
        // Check if all chunks of the file are now stored
        var file = await _fileRepository.GetByIdAsync(@event.Chunk.FileId);
        if (file != null)
        {
            _logger.LogDebug("Chunk {ChunkId} stored for file {FileId} ({FileName})", 
                @event.Chunk.Id, @event.Chunk.FileId, file.Name);
        }
        
        // TODO: Could trigger file completion check
        // TODO: Could update file status if all chunks are stored
    }

    private async Task HandleErrorStatusAsync(ChunkStatusChangedEvent @event)
    {
        _logger.LogWarning("Chunk {ChunkId} encountered an error - may need replication", @event.Chunk.Id);
        
        var file = await _fileRepository.GetByIdAsync(@event.Chunk.FileId);
        if (file != null)
        {
            _logger.LogWarning("Chunk {ChunkId} error affects file {FileId} ({FileName})", 
                @event.Chunk.Id, @event.Chunk.FileId, file.Name);
        }
        
        // TODO: Could trigger chunk replication
        // TODO: Could update file status to error
        // TODO: Could send error notifications
    }

    private async Task HandleDeletedStatusAsync(ChunkStatusChangedEvent @event)
    {
        _logger.LogInformation("Chunk {ChunkId} marked as deleted", @event.Chunk.Id);
        
        var file = await _fileRepository.GetByIdAsync(@event.Chunk.FileId);
        if (file != null)
        {
            _logger.LogInformation("Chunk {ChunkId} deleted from file {FileId} ({FileName})", 
                @event.Chunk.Id, @event.Chunk.FileId, file.Name);
        }
        
        // TODO: Could update file statistics
        // TODO: Could trigger storage cleanup
    }
}
