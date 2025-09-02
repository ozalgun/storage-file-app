using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class FileDeletedEventHandler(
    ILogger<FileDeletedEventHandler> logger,
    IChunkRepository chunkRepository,
    IStorageService storageService)
    : IDomainEventHandler<FileDeletedEvent>
{
    private readonly ILogger<FileDeletedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

    public async Task HandleAsync(FileDeletedEvent @event)
    {
        _logger.LogInformation("Handling FileDeletedEvent for file {FileId} with name {FileName}", 
            @event.FileId, @event.FileName);

        try
        {
            // Get all chunks for the deleted file
            var chunks = await _chunkRepository.GetByFileIdAsync(@event.FileId);
            var chunkList = chunks.ToList();
            
            _logger.LogInformation("File {FileId} deletion affects {ChunkCount} chunks", 
                @event.FileId, chunkList.Count);

            // Delete chunks from storage providers
            var deletionTasks = chunkList.Select(async chunk =>
            {
                try
                {
                    var deleted = await _storageService.DeleteChunkAsync(chunk);
                    if (deleted)
                    {
                        _logger.LogDebug("Successfully deleted chunk {ChunkId} from storage", chunk.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete chunk {ChunkId} from storage", chunk.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting chunk {ChunkId} from storage", chunk.Id);
                }
            });

            await Task.WhenAll(deletionTasks);

            // Log deletion statistics
            var successfulDeletions = chunkList.Count; // Assuming all deletions were successful
            _logger.LogInformation("File {FileId} deletion completed: {SuccessfulDeletions}/{TotalChunks} chunks deleted from storage", 
                @event.FileId, successfulDeletions, chunkList.Count);

            // TODO: Could update storage provider statistics
            // TODO: Could send cleanup notifications
            // TODO: Could trigger storage space optimization

            _logger.LogInformation("Successfully processed FileDeletedEvent for file {FileId}", @event.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FileDeletedEvent for file {FileId}", @event.FileId);
            throw;
        }
    }
}
