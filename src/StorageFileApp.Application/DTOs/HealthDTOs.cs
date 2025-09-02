using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.DTOs;

// Request DTOs
public record GetSystemHealthRequest(
    bool IncludeStorageProviders = true,
    bool IncludeDatabase = true,
    bool IncludeMessageQueue = true
);

public record GetStorageProviderHealthRequest(
    Guid? StorageProviderId = null,
    bool IncludeAll = false
);

public record GetSystemStatisticsRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null
);

// Response DTOs
public record SystemHealthResult(
    bool Success,
    bool IsHealthy,
    string? ErrorMessage = null,
    SystemHealthInfo? HealthInfo = null
);

public record StorageProviderHealthResult(
    bool Success,
    List<StorageProviderHealthInfo>? Providers = null,
    string? ErrorMessage = null
);

public record SystemStatisticsResult(
    bool Success,
    SystemStatisticsInfo? Statistics = null,
    string? ErrorMessage = null
);

// Supporting DTOs
public record SystemHealthInfo(
    bool DatabaseHealthy,
    bool MessageQueueHealthy,
    bool StorageProvidersHealthy,
    int TotalStorageProviders,
    int HealthyStorageProviders,
    int UnhealthyStorageProviders,
    DateTime CheckedAt
);

public record StorageProviderHealthInfo(
    Guid Id,
    string Name,
    StorageProviderType Type,
    bool IsHealthy,
    bool IsActive,
    string? ErrorMessage = null,
    long? AvailableSpace = null,
    long? TotalSpace = null,
    double? UsagePercentage = null,
    DateTime CheckedAt = default
);

public record SystemStatisticsInfo(
    int TotalFiles,
    int TotalChunks,
    long TotalStorageUsed,
    int FilesByStatus_Pending,
    int FilesByStatus_Processing,
    int FilesByStatus_Available,
    int FilesByStatus_Failed,
    int ChunksByStatus_Pending,
    int ChunksByStatus_Processing,
    int ChunksByStatus_Stored,
    int ChunksByStatus_Failed,
    DateTime GeneratedAt
);