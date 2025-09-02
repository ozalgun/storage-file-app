using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Events;

namespace StorageFileApp.Application.Events.Handlers;

public class FileCreatedEventHandler(
    ILogger<FileCreatedEventHandler> logger,
    IStorageProviderRepository storageProviderRepository)
    : IDomainEventHandler<FileCreatedEvent>
{
    private readonly ILogger<FileCreatedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));

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

            // TODO: Could trigger automatic chunking for large files
            // TODO: Could send notifications to administrators
            // TODO: Could update file statistics/metrics

            _logger.LogInformation("Successfully processed FileCreatedEvent for file {FileId}", @event.File.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FileCreatedEvent for file {FileId}", @event.File.Id);
            throw;
        }
    }
}
