using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class ChunkCreatedEventHandler(
    ILogger<ChunkCreatedEventHandler> logger,
    IStorageProviderRepository storageProviderRepository)
    : IDomainEventHandler<ChunkCreatedEvent>
{
    private readonly ILogger<ChunkCreatedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));

    public async Task HandleAsync(ChunkCreatedEvent @event)
    {
        _logger.LogInformation("Handling ChunkCreatedEvent for chunk {ChunkId} of file {FileId}", 
            @event.Chunk.Id, @event.Chunk.FileId);

        try
        {
            // Log chunk creation details
            _logger.LogInformation("Chunk created: {ChunkId} (Order: {Order}, Size: {Size} bytes, Provider: {ProviderId})", 
                @event.Chunk.Id, @event.Chunk.Order, @event.Chunk.Size, @event.Chunk.StorageProviderId);

            // Validate storage provider is still available
            var isProviderAvailable = await _storageProviderRepository.IsProviderAvailableAsync(@event.Chunk.StorageProviderId);
            
            if (!isProviderAvailable)
            {
                _logger.LogWarning("Chunk {ChunkId} assigned to unavailable storage provider {ProviderId}", 
                    @event.Chunk.Id, @event.Chunk.StorageProviderId);
            }

            // Log chunk distribution statistics
            var totalProviders = await _storageProviderRepository.GetActiveProviderCountAsync();
            _logger.LogDebug("Chunk {ChunkId} created with {TotalProviders} active storage providers available", 
                @event.Chunk.Id, totalProviders);

            // TODO: Could trigger automatic chunk storage
            // TODO: Could update chunk statistics
            // TODO: Could send chunk creation notifications

            _logger.LogInformation("Successfully processed ChunkCreatedEvent for chunk {ChunkId}", @event.Chunk.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ChunkCreatedEvent for chunk {ChunkId}", @event.Chunk.Id);
            throw;
        }
    }
}
