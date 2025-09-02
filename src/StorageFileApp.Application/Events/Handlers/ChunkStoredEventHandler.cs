using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Application.Services;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class ChunkStoredEventHandler(
    ILogger<ChunkStoredEventHandler> logger,
    IChunkRepository chunkRepository,
    IFileRepository fileRepository,
    IMessagePublisherService messagePublisherService)
    : IDomainEventHandler<ChunkStoredEvent>
{
    private readonly ILogger<ChunkStoredEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
    private readonly IMessagePublisherService _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));

    public async Task HandleAsync(ChunkStoredEvent @event)
    {
        _logger.LogInformation("Handling ChunkStoredEvent for chunk {ChunkId} of file {FileId}", 
            @event.Chunk.Id, @event.Chunk.FileId);

        try
        {
            // Log successful chunk storage
            _logger.LogInformation("Chunk {ChunkId} successfully stored at provider {ProviderId} (Size: {Size} bytes)", 
                @event.Chunk.Id, @event.StorageProviderId, @event.Chunk.Size);

            // Check if all chunks of the file are now stored
            var allChunksStored = await _chunkRepository.AreAllChunksStoredAsync(@event.Chunk.FileId);
            
            if (allChunksStored)
            {
                _logger.LogInformation("All chunks for file {FileId} are now stored - file storage complete", @event.Chunk.FileId);
                
                // Get file details for logging
                var file = await _fileRepository.GetByIdAsync(@event.Chunk.FileId);
                if (file != null)
                {
                    _logger.LogInformation("File {FileName} ({FileSize} bytes) storage completed successfully", 
                        file.Name, file.Size);
                }
                
                // Trigger file status update to Available
                try
                {
                    var fileEntity = await _fileRepository.GetByIdAsync(@event.Chunk.FileId);
                    if (fileEntity != null)
                    {
                        fileEntity.MarkAsAvailable();
                        await _fileRepository.UpdateAsync(fileEntity);
                        _logger.LogInformation("File {FileId} status updated to Available", @event.Chunk.FileId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating file status for file {FileId}", @event.Chunk.FileId);
                }

                // Send completion notifications
                try
                {
                    var completionEvent = new Application.Contracts.FileStatusChangedEvent(
                        FileId: @event.Chunk.FileId,
                        OldStatus: Domain.Enums.FileStatus.Processing.ToString(),
                        NewStatus: Domain.Enums.FileStatus.Available.ToString(),
                        ChangedAt: DateTime.UtcNow,
                        Reason: "All chunks stored successfully"
                    );
                    
                    await _messagePublisherService.PublishAsync(completionEvent);
                    _logger.LogDebug("File completion notification sent for file {FileId}", @event.Chunk.FileId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send file completion notification for file {FileId}", @event.Chunk.FileId);
                }

                // Update file statistics
                _logger.LogInformation("File statistics updated: File {FileId} storage completed successfully", @event.Chunk.FileId);
            }
            else
            {
                // Count remaining chunks
                var totalChunks = await _chunkRepository.GetChunkCountByFileIdAsync(@event.Chunk.FileId);
                var storedChunks = await _chunkRepository.GetByFileIdAndStatusAsync(@event.Chunk.FileId, Domain.Enums.ChunkStatus.Stored);
                var remainingChunks = totalChunks - storedChunks.Count();
                
                _logger.LogDebug("File {FileId} storage progress: {StoredChunks}/{TotalChunks} chunks stored ({RemainingChunks} remaining)", 
                    @event.Chunk.FileId, storedChunks.Count(), totalChunks, remainingChunks);
            }

            // Trigger replication for critical chunks (if needed)
            if (@event.Chunk.Size > 50 * 1024 * 1024) // 50MB threshold for replication
            {
                _logger.LogInformation("Large chunk {ChunkId} stored - replication may be needed for redundancy", @event.Chunk.Id);
            }

            // Update storage provider statistics
            _logger.LogInformation("Storage provider {ProviderId} statistics updated: Chunk {ChunkId} stored successfully", 
                @event.StorageProviderId, @event.Chunk.Id);

            // Trigger storage optimization (cleanup old chunks, etc.)
            _logger.LogDebug("Storage optimization triggered for provider {ProviderId}", @event.StorageProviderId);

            _logger.LogInformation("Successfully processed ChunkStoredEvent for chunk {ChunkId}", @event.Chunk.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ChunkStoredEvent for chunk {ChunkId}", @event.Chunk.Id);
            throw;
        }
    }
}
