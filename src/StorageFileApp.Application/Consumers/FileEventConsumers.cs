using MassTransit;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Contracts;
using StorageFileApp.Application.Services;
using StorageFileApp.Application.UseCases;

namespace StorageFileApp.Application.Consumers;

public class FileCreatedEventConsumer(ILogger<FileCreatedEventConsumer> logger, IFileChunkingUseCase chunkingUseCase)
    : IConsumer<FileCreatedEvent>
{
    private readonly ILogger<FileCreatedEventConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileChunkingUseCase _chunkingUseCase = chunkingUseCase ?? throw new ArgumentNullException(nameof(chunkingUseCase));

    public Task Consume(ConsumeContext<FileCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing FileCreatedEvent: FileId={FileId}, FileName={FileName}, Size={Size}", 
            message.FileId, message.FileName, message.FileSize);

        try
        {
            // Trigger chunking process for large files (> 1MB)
            if (message.FileSize > 1024 * 1024) // 1MB threshold
            {
                _logger.LogInformation("File {FileId} is large ({FileSize} bytes), triggering chunking process", 
                    message.FileId, message.FileSize);
                
                // Note: In a real implementation, you would call the chunking service here
                // For now, we'll just log the action
                _logger.LogInformation("Chunking process would be triggered for FileId={FileId}", message.FileId);
            }
            else
            {
                _logger.LogInformation("File {FileId} is small ({FileSize} bytes), no chunking needed", 
                    message.FileId, message.FileSize);
            }
            
            _logger.LogInformation("Successfully processed FileCreatedEvent for FileId={FileId}", message.FileId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FileCreatedEvent for FileId={FileId}", message.FileId);
            throw;
        }
    }
}

public class FileStatusChangedEventConsumer(ILogger<FileStatusChangedEventConsumer> logger)
    : IConsumer<FileStatusChangedEvent>
{
    private readonly ILogger<FileStatusChangedEventConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task Consume(ConsumeContext<FileStatusChangedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing FileStatusChangedEvent: FileId={FileId}, {OldStatus} -> {NewStatus}", 
            message.FileId, message.OldStatus, message.NewStatus);

        try
        {
            // Handle different status changes
            switch (message.NewStatus.ToLowerInvariant())
            {
                case "available":
                    _logger.LogInformation("File {FileId} is now available for download", message.FileId);
                    break;
                case "failed":
                    _logger.LogWarning("File {FileId} processing failed, may need manual intervention", message.FileId);
                    break;
                case "processing":
                    _logger.LogInformation("File {FileId} is being processed", message.FileId);
                    break;
                case "deleted":
                    _logger.LogInformation("File {FileId} has been deleted", message.FileId);
                    break;
                default:
                    _logger.LogInformation("File {FileId} status changed to {NewStatus}", message.FileId, message.NewStatus);
                    break;
            }
            
            _logger.LogInformation("Successfully processed FileStatusChangedEvent for FileId={FileId}", message.FileId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FileStatusChangedEvent for FileId={FileId}", message.FileId);
            throw;
        }
    }
}

public class FileDeletedEventConsumer(ILogger<FileDeletedEventConsumer> logger) : IConsumer<FileDeletedEvent>
{
    private readonly ILogger<FileDeletedEventConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task Consume(ConsumeContext<FileDeletedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing FileDeletedEvent: FileId={FileId}, FileName={FileName}", 
            message.FileId, message.FileName);

        try
        {
            // File deletion cleanup logic
            _logger.LogInformation("Cleaning up resources for deleted file {FileId}", message.FileId);
            
            // In a real implementation, you would:
            // - Clean up any cached data
            // - Update storage provider usage statistics
            // - Send notifications to interested parties
            // - Update audit logs
            
            _logger.LogInformation("Successfully processed FileDeletedEvent for FileId={FileId}", message.FileId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FileDeletedEvent for FileId={FileId}", message.FileId);
            throw;
        }
    }
}

public class ChunkCreatedEventConsumer(ILogger<ChunkCreatedEventConsumer> logger) : IConsumer<ChunkCreatedEvent>
{
    private readonly ILogger<ChunkCreatedEventConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task Consume(ConsumeContext<ChunkCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing ChunkCreatedEvent: ChunkId={ChunkId}, FileId={FileId}, Order={Order}", 
            message.ChunkId, message.FileId, message.Order);

        try
        {
            // Chunk creation processing logic
            _logger.LogInformation("Chunk {ChunkId} created for file {FileId}, order {Order}, size {Size}", 
                message.ChunkId, message.FileId, message.Order, message.Size);
            
            // In a real implementation, you would:
            // - Trigger storage process for the chunk
            // - Update chunk status to Processing
            // - Check if all chunks are created for the file
            // - Update file status accordingly
            
            _logger.LogInformation("Successfully processed ChunkCreatedEvent for ChunkId={ChunkId}", message.ChunkId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ChunkCreatedEvent for ChunkId={ChunkId}", message.ChunkId);
            throw;
        }
    }
}

public class ChunkStatusChangedEventConsumer(ILogger<ChunkStatusChangedEventConsumer> logger)
    : IConsumer<ChunkStatusChangedEvent>
{
    private readonly ILogger<ChunkStatusChangedEventConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task Consume(ConsumeContext<ChunkStatusChangedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing ChunkStatusChangedEvent: ChunkId={ChunkId}, {OldStatus} -> {NewStatus}", 
            message.ChunkId, message.OldStatus, message.NewStatus);

        try
        {
            // Chunk status change processing logic
            _logger.LogInformation("Chunk {ChunkId} status changed from {OldStatus} to {NewStatus}", 
                message.ChunkId, message.OldStatus, message.NewStatus);
            
            // Handle different chunk status changes
            switch (message.NewStatus.ToLowerInvariant())
            {
                case "stored":
                    _logger.LogInformation("Chunk {ChunkId} successfully stored", message.ChunkId);
                    break;
                case "failed":
                    _logger.LogWarning("Chunk {ChunkId} storage failed, may need replication", message.ChunkId);
                    break;
                case "unhealthy":
                    _logger.LogWarning("Chunk {ChunkId} is unhealthy, triggering replication", message.ChunkId);
                    break;
                default:
                    _logger.LogInformation("Chunk {ChunkId} status updated to {NewStatus}", message.ChunkId, message.NewStatus);
                    break;
            }
            
            _logger.LogInformation("Successfully processed ChunkStatusChangedEvent for ChunkId={ChunkId}", message.ChunkId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ChunkStatusChangedEvent for ChunkId={ChunkId}", message.ChunkId);
            throw;
        }
    }
}

public class ChunkStoredEventConsumer(ILogger<ChunkStoredEventConsumer> logger) : IConsumer<ChunkStoredEvent>
{
    private readonly ILogger<ChunkStoredEventConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task Consume(ConsumeContext<ChunkStoredEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing ChunkStoredEvent: ChunkId={ChunkId}, FileId={FileId}, ProviderId={ProviderId}", 
            message.ChunkId, message.FileId, message.StorageProviderId);

        try
        {
            // Chunk stored processing logic
            _logger.LogInformation("Chunk {ChunkId} stored successfully at {StoredPath} for file {FileId}", 
                message.ChunkId, message.StoredPath, message.FileId);
            
            // In a real implementation, you would:
            // - Update chunk status to Stored
            // - Check if all chunks are stored for the file
            // - Update file status to Available if all chunks are stored
            // - Update storage provider usage statistics
            // - Trigger file availability notifications
            
            _logger.LogInformation("Successfully processed ChunkStoredEvent for ChunkId={ChunkId}", message.ChunkId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ChunkStoredEvent for ChunkId={ChunkId}", message.ChunkId);
            throw;
        }
    }
}
