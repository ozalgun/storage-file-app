using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace StorageFileApp.Application.Services;

public class FileHealthApplicationService(
    IFileRepository fileRepository,
    IChunkRepository chunkRepository,
    IStorageProviderRepository storageProviderRepository,
    IStorageService storageService,
    IMessageQueueHealthService messageQueueHealthService,
    ILogger<FileHealthApplicationService> logger)
    : IFileHealthUseCase
{
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    private readonly IMessageQueueHealthService _messageQueueHealthService = messageQueueHealthService ?? throw new ArgumentNullException(nameof(messageQueueHealthService));
    private readonly ILogger<FileHealthApplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<SystemHealthResult> GetSystemHealthAsync(GetSystemHealthRequest request)
    {
        try
        {
            _logger.LogInformation("Checking system health...");

            // Check message queue health
            var messageQueueHealthy = true;
            if (request.IncludeMessageQueue)
            {
                messageQueueHealthy = await _messageQueueHealthService.IsHealthyAsync();
            }

            var healthInfo = new SystemHealthInfo(
                DatabaseHealthy: true, // Assume healthy if we can query
                MessageQueueHealthy: messageQueueHealthy,
                StorageProvidersHealthy: true,
                TotalStorageProviders: 0,
                HealthyStorageProviders: 0,
                UnhealthyStorageProviders: 0,
                CheckedAt: DateTime.UtcNow
            );

            if (request.IncludeStorageProviders)
            {
                var providers = await _storageProviderRepository.GetAllAsync();
                healthInfo = healthInfo with
                {
                    TotalStorageProviders = providers.Count(),
                    HealthyStorageProviders = providers.Count(p => p.IsActive),
                    UnhealthyStorageProviders = providers.Count(p => !p.IsActive)
                };

                // Check if any providers are unhealthy
                healthInfo = healthInfo with
                {
                    StorageProvidersHealthy = healthInfo.UnhealthyStorageProviders == 0
                };
            }

            var isHealthy = healthInfo.DatabaseHealthy && 
                           healthInfo.MessageQueueHealthy && 
                           healthInfo.StorageProvidersHealthy;

            _logger.LogInformation("System health check completed. Healthy: {IsHealthy}", isHealthy);

            return new SystemHealthResult(true, isHealthy, HealthInfo: healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking system health");
            return new SystemHealthResult(false, false, ErrorMessage: ex.Message);
        }
    }

    public async Task<StorageProviderHealthResult> GetStorageProviderHealthAsync(GetStorageProviderHealthRequest request)
    {
        try
        {
            _logger.LogInformation("Checking storage provider health...");

            var providers = new List<StorageProviderHealthInfo>();

            if (request.IncludeAll)
            {
                var allProviders = await _storageProviderRepository.GetAllAsync();
                foreach (var provider in allProviders)
                {
                    var healthInfo = await CheckStorageProviderHealthAsync(provider);
                    providers.Add(healthInfo);
                }
            }
            else if (request.StorageProviderId.HasValue)
            {
                var provider = await _storageProviderRepository.GetByIdAsync(request.StorageProviderId.Value);
                if (provider != null)
                {
                    var healthInfo = await CheckStorageProviderHealthAsync(provider);
                    providers.Add(healthInfo);
                }
            }

            _logger.LogInformation("Storage provider health check completed for {ProviderCount} providers", providers.Count);

            return new StorageProviderHealthResult(true, Providers: providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking storage provider health");
            return new StorageProviderHealthResult(false, ErrorMessage: ex.Message);
        }
    }

    public async Task<SystemStatisticsResult> GetSystemStatisticsAsync(GetSystemStatisticsRequest request)
    {
        try
        {
            _logger.LogInformation("Generating system statistics...");

            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            // Get file statistics
            var totalFiles = await _fileRepository.GetCountAsync();
            var filesByStatus = new Dictionary<FileStatus, int>();
            foreach (FileStatus status in Enum.GetValues<FileStatus>())
            {
                var files = await _fileRepository.GetByStatusAsync(status);
                filesByStatus[status] = files.Count();
            }

            // Get chunk statistics
            var totalChunks = await _chunkRepository.GetCountAsync();
            var chunksByStatus = new Dictionary<ChunkStatus, int>();
            foreach (ChunkStatus status in Enum.GetValues<ChunkStatus>())
            {
                var chunks = await _chunkRepository.GetByStatusAsync(status);
                chunksByStatus[status] = chunks.Count();
            }

            // Calculate total storage used
            var allChunks = await _chunkRepository.GetAllAsync();
            var totalStorageUsed = allChunks.Sum(c => c.Size);

            var statistics = new SystemStatisticsInfo(
                TotalFiles: totalFiles,
                TotalChunks: totalChunks,
                TotalStorageUsed: totalStorageUsed,
                FilesByStatus_Pending: filesByStatus.GetValueOrDefault(FileStatus.Pending, 0),
                FilesByStatus_Processing: filesByStatus.GetValueOrDefault(FileStatus.Processing, 0),
                FilesByStatus_Available: filesByStatus.GetValueOrDefault(FileStatus.Available, 0),
                FilesByStatus_Failed: filesByStatus.GetValueOrDefault(FileStatus.Failed, 0),
                ChunksByStatus_Pending: chunksByStatus.GetValueOrDefault(ChunkStatus.Pending, 0),
                ChunksByStatus_Processing: chunksByStatus.GetValueOrDefault(ChunkStatus.Processing, 0),
                ChunksByStatus_Stored: chunksByStatus.GetValueOrDefault(ChunkStatus.Stored, 0),
                ChunksByStatus_Failed: chunksByStatus.GetValueOrDefault(ChunkStatus.Failed, 0),
                GeneratedAt: DateTime.UtcNow
            );

            _logger.LogInformation("System statistics generated successfully");

            return new SystemStatisticsResult(true, Statistics: statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating system statistics");
            return new SystemStatisticsResult(false, ErrorMessage: ex.Message);
        }
    }

    private async Task<StorageProviderHealthInfo> CheckStorageProviderHealthAsync(Domain.Entities.StorageProviderEntity.StorageProvider provider)
    {
        try
        {
            var isHealthy = await _storageService.IsProviderHealthyAsync(provider);
            var availableSpace = await _storageService.GetAvailableSpaceAsync(provider);
            
            return new StorageProviderHealthInfo(
                Id: provider.Id,
                Name: provider.Name,
                Type: provider.Type,
                IsHealthy: isHealthy,
                IsActive: provider.IsActive,
                AvailableSpace: availableSpace,
                CheckedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking health for storage provider {ProviderId}", provider.Id);
            return new StorageProviderHealthInfo(
                Id: provider.Id,
                Name: provider.Name,
                Type: provider.Type,
                IsHealthy: false,
                IsActive: provider.IsActive,
                ErrorMessage: ex.Message,
                CheckedAt: DateTime.UtcNow
            );
        }
    }
}