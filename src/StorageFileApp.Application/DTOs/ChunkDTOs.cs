using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Services;

namespace StorageFileApp.Application.DTOs;

// Request DTOs
public record ChunkFileRequest(
    Guid FileId,
    byte[]? FileBytes = null,
    Stream? FileStream = null,
    long? ChunkSize = null,
    StorageStrategy? Strategy = null
);

public record MergeChunksRequest(
    Guid FileId,
    string? OutputPath = null,
    bool ValidateIntegrity = true
);

public record ValidateChunksRequest(
    Guid FileId,
    bool ValidateChecksums = true,
    bool ValidateSequence = true
);

// Response DTOs
public record ChunkingResult(
    bool Success,
    List<ChunkInfo>? Chunks = null,
    string? ErrorMessage = null,
    TimeSpan? ProcessingTime = null
);

public record MergingResult(
    bool Success,
    string? OutputPath = null,
    string? ErrorMessage = null,
    TimeSpan? ProcessingTime = null,
    bool? IntegrityValid = null
);

public record ChunkValidationResult(
    bool Success,
    List<ChunkValidationInfo>? ValidationResults = null,
    string? ErrorMessage = null,
    bool? AllChunksValid = null,
    TimeSpan? ProcessingTime = null
);

// Supporting DTOs
public record ChunkInfo(
    Guid Id,
    int Order,
    long Size,
    Guid StorageProviderId,
    ChunkStatus Status,
    DateTime CreatedAt
);

public record ChunkValidationInfo(
    Guid ChunkId,
    int Order,
    bool IsValid,
    string? ErrorMessage = null,
    string? Checksum = null
);
