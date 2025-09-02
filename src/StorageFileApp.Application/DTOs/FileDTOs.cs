using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Application.DTOs;

// Request DTOs
public record StoreFileRequest(
    string FileName,
    long FileSize,
    string ContentType,
    string? Description = null,
    Dictionary<string, string>? CustomProperties = null
);

public record RetrieveFileRequest(
    Guid FileId,
    string? OutputPath = null
);

public record DeleteFileRequest(
    Guid FileId,
    bool ForceDelete = false
);

public record GetFileStatusRequest(
    Guid FileId
);

public record ListFilesRequest(
    int PageNumber = 1,
    int PageSize = 20,
    FileStatus? Status = null,
    string? SearchTerm = null
);

// Response DTOs
public record FileStorageResult(
    bool Success,
    Guid? FileId = null,
    string? ErrorMessage = null,
    List<string>? Warnings = null
);

public record FileRetrievalResult(
    bool Success,
    string? FilePath = null,
    string? ErrorMessage = null,
    FileMetadata? FileMetadata = null
);

public record FileDeletionResult(
    bool Success,
    string? ErrorMessage = null
);

public record FileStatusResult(
    bool Success,
    FileStatus? Status = null,
    string? ErrorMessage = null,
    DateTime? LastUpdated = null
);

public record FileListResult(
    bool Success,
    List<FileSummary>? Files = null,
    int? TotalCount = null,
    int? PageNumber = null,
    int? PageSize = null,
    string? ErrorMessage = null
);

// Supporting DTOs
public record FileSummary(
    Guid Id,
    string Name,
    long Size,
    FileStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record FileMetadata(
    string ContentType,
    string? Description,
    Dictionary<string, string> CustomProperties
);
