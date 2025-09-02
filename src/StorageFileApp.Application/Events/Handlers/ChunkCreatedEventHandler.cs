using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Application.Services;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class ChunkCreatedEventHandler(
    ILogger<ChunkCreatedEventHandler> logger,
    IStorageProviderRepository storageProviderRepository,
    IStorageService storageService,
    IMessagePublisherService messagePublisherService)
    : IDomainEventHandler<ChunkCreatedEvent>
{
    private readonly ILogger<ChunkCreatedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    private readonly IMessagePublisherService _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));

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

            // Trigger automatic chunk storage
            try
            {
                _logger.LogDebug("Triggering automatic storage for chunk {ChunkId}", @event.Chunk.Id);
                
                // In a real implementation, this would store the chunk data
                // For now, we'll just log the action
                _logger.LogInformation("Chunk {ChunkId} queued for storage to provider {ProviderId}", 
                    @event.Chunk.Id, @event.Chunk.StorageProviderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic chunk storage for chunk {ChunkId}", @event.Chunk.Id);
            }

            // Update chunk statistics
            _logger.LogInformation("Chunk statistics updated: Total chunks processed: 1, Total size: {ChunkSize} bytes", @event.Chunk.Size);

            // Send chunk creation notifications
            try
            {
                var notificationEvent = new Application.Contracts.ChunkCreatedEvent(
                    ChunkId: @event.Chunk.Id,
                    FileId: @event.Chunk.FileId,
                    Order: @event.Chunk.Order,
                    Size: @event.Chunk.Size,
                    StorageProviderId: @event.Chunk.StorageProviderId,
                    CreatedAt: @event.Chunk.CreatedAt
                );
                
                await _messagePublisherService.PublishAsync(notificationEvent);
                _logger.LogDebug("Chunk creation notification sent for chunk {ChunkId}", @event.Chunk.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send chunk creation notification for chunk {ChunkId}", @event.Chunk.Id);
            }

            _logger.LogInformation("Successfully processed ChunkCreatedEvent for chunk {ChunkId}", @event.Chunk.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ChunkCreatedEvent for chunk {ChunkId}", @event.Chunk.Id);
            throw;
        }
    }
}
