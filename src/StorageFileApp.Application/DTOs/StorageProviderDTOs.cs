using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.DTOs;

// Request DTOs
public record RegisterStorageProviderRequest(
    string Name,
    StorageProviderType Type,
    string ConnectionString,
    bool IsActive = true
);

public record UpdateStorageProviderRequest(
    Guid Id,
    string? Name = null,
    string? ConnectionString = null,
    bool? IsActive = null
);

public record DeleteStorageProviderRequest(
    Guid Id,
    bool ForceDelete = false
);

public record CheckStorageProviderHealthRequest(
    Guid Id
);

// Response DTOs
public record StorageProviderResult(
    bool Success,
    StorageProviderInfo? Provider = null,
    string? ErrorMessage = null
);

public record StorageProviderDeletionResult(
    bool Success,
    string? ErrorMessage = null
);

public record StorageProviderListResult(
    bool Success,
    List<StorageProviderInfo>? Providers = null,
    string? ErrorMessage = null
);

// Supporting DTOs
public record StorageProviderInfo(
    Guid Id,
    string Name,
    StorageProviderType Type,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
