using MassTransit;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Contracts;

namespace StorageFileApp.Application.Services;

public interface IMessagePublisherService
{
    Task PublishAsync<T>(T message) where T : class;
    Task PublishFileCreatedAsync(Guid fileId, string fileName, long fileSize, string contentType, string checksum);
    Task PublishFileStatusChangedAsync(Guid fileId, string fileName, string oldStatus, string newStatus);
    Task PublishFileDeletedAsync(Guid fileId, string fileName);
    Task PublishChunkCreatedAsync(Guid chunkId, Guid fileId, int order, long size, string checksum, Guid storageProviderId);
    Task PublishChunkStatusChangedAsync(Guid chunkId, Guid fileId, string oldStatus, string newStatus);
    Task PublishChunkStoredAsync(Guid chunkId, Guid fileId, Guid storageProviderId, string storedPath);
    Task PublishStorageProviderHealthCheckAsync(Guid storageProviderId, string providerName, bool isHealthy, string? errorMessage = null);
    Task PublishStorageProviderSpaceWarningAsync(Guid storageProviderId, string providerName, long availableSpace, long totalSpace);
}

public class MessagePublisherService(IPublishEndpoint publishEndpoint, ILogger<MessagePublisherService> logger)
    : IMessagePublisherService
{
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    private readonly ILogger<MessagePublisherService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task PublishAsync<T>(T message) where T : class
    {
        await _publishEndpoint.Publish(message);
        _logger.LogDebug("Published message of type {MessageType}", typeof(T).Name);
    }

    public async Task PublishFileCreatedAsync(Guid fileId, string fileName, long fileSize, string contentType, string checksum)
    {
        var message = new FileCreatedEvent(
            FileId: fileId,
            FileName: fileName,
            FileSize: fileSize,
            CreatedAt: DateTime.UtcNow,
            ContentType: contentType
        );

        await _publishEndpoint.Publish(message);
        _logger.LogInformation("Published FileCreatedEvent: FileId={FileId}, FileName={FileName}", fileId, fileName);
    }

    public async Task PublishFileStatusChangedAsync(Guid fileId, string fileName, string oldStatus, string newStatus)
    {
        var message = new FileStatusChangedEvent(
            FileId: fileId,
            OldStatus: oldStatus,
            NewStatus: newStatus,
            ChangedAt: DateTime.UtcNow
        );

        await _publishEndpoint.Publish(message);
        _logger.LogInformation("Published FileStatusChangedEvent: FileId={FileId}, {OldStatus} -> {NewStatus}", 
            fileId, oldStatus, newStatus);
    }

    public async Task PublishFileDeletedAsync(Guid fileId, string fileName)
    {
        var message = new FileDeletedEvent(
            FileId: fileId,
            FileName: fileName,
            DeletedAt: DateTime.UtcNow,
            ChunkCount: 0 // This would need to be passed as parameter
        );

        await _publishEndpoint.Publish(message);
        _logger.LogInformation("Published FileDeletedEvent: FileId={FileId}, FileName={FileName}", fileId, fileName);
    }

    public async Task PublishChunkCreatedAsync(Guid chunkId, Guid fileId, int order, long size, string checksum, Guid storageProviderId)
    {
        var message = new ChunkCreatedEvent(
            ChunkId: chunkId,
            FileId: fileId,
            Order: order,
            Size: size,
            StorageProviderId: storageProviderId,
            CreatedAt: DateTime.UtcNow
        );

        await _publishEndpoint.Publish(message);
        _logger.LogInformation("Published ChunkCreatedEvent: ChunkId={ChunkId}, FileId={FileId}, Order={Order}", 
            chunkId, fileId, order);
    }

    public async Task PublishChunkStatusChangedAsync(Guid chunkId, Guid fileId, string oldStatus, string newStatus)
    {
        var message = new ChunkStatusChangedEvent(
            ChunkId: chunkId,
            FileId: fileId,
            OldStatus: oldStatus,
            NewStatus: newStatus,
            ChangedAt: DateTime.UtcNow
        );

        await _publishEndpoint.Publish(message);
        _logger.LogInformation("Published ChunkStatusChangedEvent: ChunkId={ChunkId}, {OldStatus} -> {NewStatus}", 
            chunkId, oldStatus, newStatus);
    }

    public async Task PublishChunkStoredAsync(Guid chunkId, Guid fileId, Guid storageProviderId, string storedPath)
    {
        var message = new ChunkStoredEvent
        {
            ChunkId = chunkId,
            FileId = fileId,
            StorageProviderId = storageProviderId,
            StoredPath = storedPath,
            StoredAt = DateTime.UtcNow
        };

        await _publishEndpoint.Publish(message);
        _logger.LogInformation("Published ChunkStoredEvent: ChunkId={ChunkId}, FileId={FileId}, ProviderId={ProviderId}", 
            chunkId, fileId, storageProviderId);
    }

    public async Task PublishStorageProviderHealthCheckAsync(Guid storageProviderId, string providerName, bool isHealthy, string? errorMessage = null)
    {
        var message = new StorageProviderHealthCheckEvent
        {
            StorageProviderId = storageProviderId,
            ProviderName = providerName,
            IsHealthy = isHealthy,
            ErrorMessage = errorMessage,
            CheckedAt = DateTime.UtcNow
        };

        await _publishEndpoint.Publish(message);
        _logger.LogInformation("Published StorageProviderHealthCheckEvent: ProviderId={ProviderId}, IsHealthy={IsHealthy}", 
            storageProviderId, isHealthy);
    }

    public async Task PublishStorageProviderSpaceWarningAsync(Guid storageProviderId, string providerName, long availableSpace, long totalSpace)
    {
        var usagePercentage = totalSpace > 0 ? (double)(totalSpace - availableSpace) / totalSpace * 100 : 0;
        
        var message = new StorageProviderSpaceWarningEvent
        {
            StorageProviderId = storageProviderId,
            ProviderName = providerName,
            AvailableSpace = availableSpace,
            TotalSpace = totalSpace,
            UsagePercentage = usagePercentage,
            WarnedAt = DateTime.UtcNow
        };

        await _publishEndpoint.Publish(message);
        _logger.LogWarning("Published StorageProviderSpaceWarningEvent: ProviderId={ProviderId}, Usage={UsagePercentage:F1}%", 
            storageProviderId, usagePercentage);
    }
}
