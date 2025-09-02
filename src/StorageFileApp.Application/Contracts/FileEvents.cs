using System;

namespace StorageFileApp.Application.Contracts;

// File Events
public record FileCreatedEvent
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Checksum { get; init; } = string.Empty;
}

public record FileStatusChangedEvent
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
}

public record FileDeletedEvent
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; }
}

// Chunk Events
public record ChunkCreatedEvent
{
    public Guid ChunkId { get; init; }
    public Guid FileId { get; init; }
    public int Order { get; init; }
    public long Size { get; init; }
    public string Checksum { get; init; } = string.Empty;
    public Guid StorageProviderId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ChunkStatusChangedEvent
{
    public Guid ChunkId { get; init; }
    public Guid FileId { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
}

public record ChunkStoredEvent
{
    public Guid ChunkId { get; init; }
    public Guid FileId { get; init; }
    public Guid StorageProviderId { get; init; }
    public string StoredPath { get; init; } = string.Empty;
    public DateTime StoredAt { get; init; }
}

// Storage Provider Events
public record StorageProviderHealthCheckEvent
{
    public Guid StorageProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public bool IsHealthy { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CheckedAt { get; init; }
}

public record StorageProviderSpaceWarningEvent
{
    public Guid StorageProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public long AvailableSpace { get; init; }
    public long TotalSpace { get; init; }
    public double UsagePercentage { get; init; }
    public DateTime WarnedAt { get; init; }
}
