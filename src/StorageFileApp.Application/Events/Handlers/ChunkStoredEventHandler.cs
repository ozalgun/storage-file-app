using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class ChunkStoredEventHandler(
    ILogger<ChunkStoredEventHandler> logger,
    IChunkRepository chunkRepository,
    IFileRepository fileRepository)
    : IDomainEventHandler<ChunkStoredEvent>
{
    private readonly ILogger<ChunkStoredEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));

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
                
                // TODO: Could trigger file status update to Stored
                // TODO: Could send completion notifications
                // TODO: Could update file statistics
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

            // TODO: Could trigger replication for critical chunks
            // TODO: Could update storage provider statistics
            // TODO: Could trigger storage optimization

            _logger.LogInformation("Successfully processed ChunkStoredEvent for chunk {ChunkId}", @event.Chunk.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ChunkStoredEvent for chunk {ChunkId}", @event.Chunk.Id);
            throw;
        }
    }
}
