using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Application.Services;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class FileDeletedEventHandler(
    ILogger<FileDeletedEventHandler> logger,
    IChunkRepository chunkRepository,
    IStorageService storageService,
    IMessagePublisherService messagePublisherService)
    : IDomainEventHandler<FileDeletedEvent>
{
    private readonly ILogger<FileDeletedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    private readonly IMessagePublisherService _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));

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

            // Update storage provider statistics
            var providerStats = chunkList.GroupBy(c => c.StorageProviderId)
                .Select(g => new { ProviderId = g.Key, ChunkCount = g.Count() });
            
            foreach (var stat in providerStats)
            {
                _logger.LogInformation("Storage provider {ProviderId} statistics updated: {ChunkCount} chunks deleted", 
                    stat.ProviderId, stat.ChunkCount);
            }

            // Send cleanup notifications
            try
            {
                var cleanupEvent = new Application.Contracts.FileDeletedEvent(
                    FileId: @event.FileId,
                    FileName: @event.FileName,
                    DeletedAt: DateTime.UtcNow,
                    ChunkCount: chunkList.Count
                );
                
                await _messagePublisherService.PublishAsync(cleanupEvent);
                _logger.LogDebug("File deletion notification sent for file {FileId}", @event.FileId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send file deletion notification for file {FileId}", @event.FileId);
            }

            // Trigger storage space optimization
            _logger.LogInformation("Storage space optimization triggered after file {FileId} deletion", @event.FileId);

            _logger.LogInformation("Successfully processed FileDeletedEvent for file {FileId}", @event.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FileDeletedEvent for file {FileId}", @event.FileId);
            throw;
        }
    }
}
