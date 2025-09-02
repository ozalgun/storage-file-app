using StorageFileApp.Application.DTOs;

namespace StorageFileApp.Application.UseCases;

public interface IFileHealthUseCase
{
    Task<SystemHealthResult> GetSystemHealthAsync(GetSystemHealthRequest request);
    Task<StorageProviderHealthResult> GetStorageProviderHealthAsync(GetStorageProviderHealthRequest request);
    Task<SystemStatisticsResult> GetSystemStatisticsAsync(GetSystemStatisticsRequest request);
}
