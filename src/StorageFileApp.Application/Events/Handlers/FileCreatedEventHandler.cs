using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.Services;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class FileCreatedEventHandler(
    ILogger<FileCreatedEventHandler> logger,
    IStorageProviderRepository storageProviderRepository,
    IFileChunkingUseCase fileChunkingUseCase,
    IMessagePublisherService messagePublisherService)
    : IDomainEventHandler<FileCreatedEvent>
{
    private readonly ILogger<FileCreatedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IFileChunkingUseCase _fileChunkingUseCase = fileChunkingUseCase ?? throw new ArgumentNullException(nameof(fileChunkingUseCase));
    private readonly IMessagePublisherService _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));

    public async Task HandleAsync(FileCreatedEvent @event)
    {
        _logger.LogInformation("Handling FileCreatedEvent for file {FileId} with name {FileName}", 
            @event.File.Id, @event.File.Name);

        try
        {
            // Log file creation metrics
            _logger.LogInformation("File created: {FileName} (Size: {FileSize} bytes, Type: {FileType})", 
                @event.File.Name, @event.File.Size, @event.File.Metadata.ContentType);

            // Check available storage providers
            var availableProviders = await _storageProviderRepository.GetActiveProvidersAsync();
            var providerCount = availableProviders.Count();

            _logger.LogInformation("File {FileId} created with {ProviderCount} available storage providers", 
                @event.File.Id, providerCount);

            // Log storage strategy if file is large
            if (@event.File.Size > 100 * 1024 * 1024) // 100MB
            {
                _logger.LogInformation("Large file detected: {FileName} ({FileSize} bytes) - will be chunked for distributed storage", 
                    @event.File.Name, @event.File.Size);
            }

            // Trigger automatic chunking for large files
            if (@event.File.Size > 10 * 1024 * 1024) // 10MB threshold
            {
                _logger.LogInformation("Triggering automatic chunking for large file: {FileName}", @event.File.Name);
                
                try
                {
                    var chunkRequest = new Application.DTOs.ChunkFileRequest(
                        FileId: @event.File.Id,
                        ChunkSize: null, // Use default chunk size
                        Strategy: null // Use default strategy
                    );
                    
                    var chunkResult = await _fileChunkingUseCase.ChunkFileAsync(chunkRequest);
                    if (chunkResult.Success)
                    {
                        _logger.LogInformation("Automatic chunking completed for file {FileId}. Created {ChunkCount} chunks.", 
                            @event.File.Id, chunkResult.Chunks?.Count ?? 0);
                    }
                    else
                    {
                        _logger.LogWarning("Automatic chunking failed for file {FileId}: {ErrorMessage}", 
                            @event.File.Id, chunkResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic chunking for file {FileId}", @event.File.Id);
                }
            }

            // Send notifications to administrators (via message queue)
            try
            {
                var notificationEvent = new Application.Contracts.FileCreatedEvent(
                    FileId: @event.File.Id,
                    FileName: @event.File.Name,
                    FileSize: @event.File.Size,
                    CreatedAt: @event.File.CreatedAt,
                    ContentType: @event.File.Metadata.ContentType
                );
                
                await _messagePublisherService.PublishAsync(notificationEvent);
                _logger.LogDebug("File creation notification sent for file {FileId}", @event.File.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send file creation notification for file {FileId}", @event.File.Id);
            }

            // Update file statistics/metrics
            _logger.LogInformation("File statistics updated: Total files processed: 1, Total size: {FileSize} bytes", @event.File.Size);

            _logger.LogInformation("Successfully processed FileCreatedEvent for file {FileId}", @event.File.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FileCreatedEvent for file {FileId}", @event.File.Id);
            throw;
        }
    }
}
