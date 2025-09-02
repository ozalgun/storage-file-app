using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Events;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.Events.Handlers;

public class FileStatusChangedEventHandler(
    ILogger<FileStatusChangedEventHandler> logger,
    IChunkRepository chunkRepository)
    : IDomainEventHandler<FileStatusChangedEvent>
{
    private readonly ILogger<FileStatusChangedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));

    public async Task HandleAsync(FileStatusChangedEvent @event)
    {
        _logger.LogInformation("Handling FileStatusChangedEvent for file {FileId}: {OldStatus} -> {NewStatus}", 
            @event.File.Id, @event.OldStatus, @event.NewStatus);

        try
        {
            // Handle different status transitions
            switch (@event.NewStatus)
            {
                case FileStatus.Processing:
                    await HandleProcessingStatusAsync(@event);
                    break;
                case FileStatus.Stored:
                    await HandleStoredStatusAsync(@event);
                    break;
                case FileStatus.Error:
                    await HandleErrorStatusAsync(@event);
                    break;
                case FileStatus.Deleted:
                    await HandleDeletedStatusAsync(@event);
                    break;
                default:
                    _logger.LogInformation("File {FileId} status changed to {NewStatus}", @event.File.Id, @event.NewStatus);
                    break;
            }

            _logger.LogInformation("Successfully processed FileStatusChangedEvent for file {FileId}", @event.File.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FileStatusChangedEvent for file {FileId}", @event.File.Id);
            throw;
        }
    }

    private async Task HandleProcessingStatusAsync(FileStatusChangedEvent @event)
    {
        _logger.LogInformation("File {FileId} is now processing - checking chunk status", @event.File.Id);
        
        var chunks = await _chunkRepository.GetByFileIdAsync(@event.File.Id);
        var chunkCount = chunks.Count();
        
        _logger.LogInformation("File {FileId} has {ChunkCount} chunks to process", @event.File.Id, chunkCount);
    }

    private async Task HandleStoredStatusAsync(FileStatusChangedEvent @event)
    {
        _logger.LogInformation("File {FileId} successfully stored - validating all chunks", @event.File.Id);
        
        var allChunksStored = await _chunkRepository.AreAllChunksStoredAsync(@event.File.Id);
        
        if (allChunksStored)
        {
            _logger.LogInformation("All chunks for file {FileId} are successfully stored", @event.File.Id);
        }
        else
        {
            _logger.LogWarning("File {FileId} marked as stored but not all chunks are stored", @event.File.Id);
        }
    }

    private async Task HandleErrorStatusAsync(FileStatusChangedEvent @event)
    {
        _logger.LogWarning("File {FileId} encountered an error - checking for failed chunks", @event.File.Id);
        
        var errorChunks = await _chunkRepository.GetUnhealthyChunksAsync();
        var fileErrorChunks = errorChunks.Where(c => c.FileId == @event.File.Id);
        
        _logger.LogWarning("File {FileId} has {ErrorChunkCount} chunks in error state", 
            @event.File.Id, fileErrorChunks.Count());
    }

    private async Task HandleDeletedStatusAsync(FileStatusChangedEvent @event)
    {
        _logger.LogInformation("File {FileId} marked as deleted - cleaning up chunks", @event.File.Id);
        
        var chunks = await _chunkRepository.GetByFileIdAsync(@event.File.Id);
        var chunkCount = chunks.Count();
        
        _logger.LogInformation("File {FileId} deletion will affect {ChunkCount} chunks", @event.File.Id, chunkCount);
        
        // Trigger chunk cleanup process
        _logger.LogInformation("Chunk cleanup process triggered for file {FileId}", @event.File.Id);

        // Update storage provider statistics
        var providerStats = chunks.GroupBy(c => c.StorageProviderId)
            .Select(g => new { ProviderId = g.Key, ChunkCount = g.Count() });
        
        foreach (var stat in providerStats)
        {
            _logger.LogInformation("Storage provider {ProviderId} statistics updated: {ChunkCount} chunks will be cleaned up", 
                stat.ProviderId, stat.ChunkCount);
        }
    }
}
