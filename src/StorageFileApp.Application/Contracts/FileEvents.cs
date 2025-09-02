using System;

namespace StorageFileApp.Application.Contracts;

// File Events
public record FileCreatedEvent(
    Guid FileId,
    string FileName,
    long FileSize,
    DateTime CreatedAt,
    string ContentType
);

public record FileStatusChangedEvent(
    Guid FileId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt,
    string? Reason = null
);

public record FileDeletedEvent(
    Guid FileId,
    string FileName,
    DateTime DeletedAt,
    int ChunkCount
);

// Chunk Events
public record ChunkCreatedEvent(
    Guid ChunkId,
    Guid FileId,
    int Order,
    long Size,
    Guid StorageProviderId,
    DateTime CreatedAt
);

public record ChunkStatusChangedEvent(
    Guid ChunkId,
    Guid FileId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt,
    string? Reason = null
);

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
