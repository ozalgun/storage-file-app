using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Application.Services;
using StorageFileApp.Domain.Events;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.Events.Handlers;

public class ChunkStatusChangedEventHandler(
    ILogger<ChunkStatusChangedEventHandler> logger,
    IFileRepository fileRepository,
    IChunkRepository chunkRepository,
    IMessagePublisherService messagePublisherService)
    : IDomainEventHandler<ChunkStatusChangedEvent>
{
    private readonly ILogger<ChunkStatusChangedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IMessagePublisherService _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));

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
        
        // Trigger file completion check
        try
        {
            var allChunksStored = await _chunkRepository.AreAllChunksStoredAsync(@event.Chunk.FileId);
            if (allChunksStored)
            {
                var fileEntity = await _fileRepository.GetByIdAsync(@event.Chunk.FileId);
                if (fileEntity != null)
                {
                    fileEntity.MarkAsAvailable();
                    await _fileRepository.UpdateAsync(fileEntity);
                    _logger.LogInformation("File {FileId} status updated to Available - all chunks stored", @event.Chunk.FileId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file completion for file {FileId}", @event.Chunk.FileId);
        }
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
        
        // Trigger chunk replication
        _logger.LogInformation("Chunk {ChunkId} error detected - replication may be needed", @event.Chunk.Id);

        // Update file status to error if critical
        try
        {
            var fileEntity = await _fileRepository.GetByIdAsync(@event.Chunk.FileId);
            if (fileEntity != null)
            {
                // Only mark as failed if this is a critical chunk or multiple chunks have failed
                var failedChunks = await _chunkRepository.GetByFileIdAndStatusAsync(@event.Chunk.FileId, ChunkStatus.Failed);
                if (failedChunks.Count() > 1)
                {
                    fileEntity.MarkAsFailed();
                    await _fileRepository.UpdateAsync(fileEntity);
                    _logger.LogWarning("File {FileId} marked as Failed due to multiple chunk errors", @event.Chunk.FileId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file status for file {FileId}", @event.Chunk.FileId);
        }

        // Send error notifications
        try
        {
            var errorEvent = new Application.Contracts.ChunkStatusChangedEvent(
                ChunkId: @event.Chunk.Id,
                FileId: @event.Chunk.FileId,
                OldStatus: @event.OldStatus.ToString(),
                NewStatus: @event.NewStatus.ToString(),
                ChangedAt: DateTime.UtcNow,
                Reason: "Chunk storage error"
            );
            
            await _messagePublisherService.PublishAsync(errorEvent);
            _logger.LogDebug("Chunk error notification sent for chunk {ChunkId}", @event.Chunk.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send chunk error notification for chunk {ChunkId}", @event.Chunk.Id);
        }
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
        
        // Update file statistics
        _logger.LogInformation("File statistics updated: Chunk {ChunkId} deleted from file {FileId}", 
            @event.Chunk.Id, @event.Chunk.FileId);

        // Trigger storage cleanup
        _logger.LogInformation("Storage cleanup triggered after chunk {ChunkId} deletion", @event.Chunk.Id);
    }
}
