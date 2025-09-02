using StorageFileApp.Application.DTOs;

namespace StorageFileApp.Application.UseCases;

public interface IFileStorageUseCase
{
    Task<FileStorageResult> StoreFileAsync(StoreFileRequest request);
    Task<FileRetrievalResult> RetrieveFileAsync(RetrieveFileRequest request);
    Task<FileDeletionResult> DeleteFileAsync(DeleteFileRequest request);
    Task<FileStatusResult> GetFileStatusAsync(GetFileStatusRequest request);
    Task<FileListResult> ListFilesAsync(ListFilesRequest request);
}

public interface IFileChunkingUseCase
{
    Task<ChunkingResult> ChunkFileAsync(ChunkFileRequest request);
    Task<MergingResult> MergeChunksAsync(MergeChunksRequest request);
    Task<ChunkValidationResult> ValidateChunksAsync(ValidateChunksRequest request);
}

public interface IStorageProviderUseCase
{
    Task<StorageProviderResult> RegisterStorageProviderAsync(RegisterStorageProviderRequest request);
    Task<StorageProviderResult> UpdateStorageProviderAsync(UpdateStorageProviderRequest request);
    Task<StorageProviderDeletionResult> DeleteStorageProviderAsync(DeleteStorageProviderRequest request);
    Task<StorageProviderListResult> ListStorageProvidersAsync();
    Task<StorageProviderHealthResult> CheckStorageProviderHealthAsync(CheckStorageProviderHealthRequest request);
}

public interface IFileHealthUseCase
{
    Task<FileHealthResult> CheckFileHealthAsync(CheckFileHealthRequest request);
    Task<ChunkHealthResult> CheckChunkHealthAsync(CheckChunkHealthRequest request);
    Task<ReplicationResult> ReplicateChunksAsync(ReplicateChunksRequest request);
    Task<HealthReportResult> GenerateHealthReportAsync();
}
