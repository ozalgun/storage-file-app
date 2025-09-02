using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Services;

namespace StorageFileApp.Application.DTOs;

// Request DTOs
public record CheckFileHealthRequest(
    Guid FileId
);

public record CheckChunkHealthRequest(
    Guid FileId,
    bool CheckAllChunks = true
);

public record ReplicateChunksRequest(
    Guid FileId,
    int? ReplicationCount = null,
    ReplicationPriority? Priority = null
);

// Response DTOs
public record FileHealthResult(
    bool Success,
    FileHealthInfo? HealthInfo = null,
    string? ErrorMessage = null
);

public record ChunkHealthResult(
    bool Success,
    List<ChunkHealthInfo>? ChunkHealthInfos = null,
    string? ErrorMessage = null
);

public record ReplicationResult(
    bool Success,
    List<ReplicationInfo>? Replications = null,
    string? ErrorMessage = null
);

public record HealthReportResult(
    bool Success,
    SystemHealthReport? Report = null,
    string? ErrorMessage = null
);

// Supporting DTOs
public record FileHealthInfo(
    Guid FileId,
    bool IsHealthy,
    int TotalChunks,
    int HealthyChunks,
    int UnhealthyChunks,
    int ReplicatedChunks,
    DateTime LastChecked
);

public record ChunkHealthInfo(
    Guid ChunkId,
    ChunkHealthStatus Status,
    bool NeedsReplication,
    DateTime LastChecked,
    string? ErrorMessage = null
);

public record ReplicationInfo(
    Guid ChunkId,
    Guid SourceProviderId,
    Guid TargetProviderId,
    bool Success,
    DateTime ReplicatedAt,
    string? ErrorMessage = null
);

public record SystemHealthReport(
    int TotalFiles,
    int HealthyFiles,
    int UnhealthyFiles,
    int TotalChunks,
    int HealthyChunks,
    int UnhealthyChunks,
    int TotalStorageProviders,
    int ActiveStorageProviders,
    DateTime GeneratedAt
);
