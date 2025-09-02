using Microsoft.Extensions.Logging;
using StorageFileApp.Application.UseCases;
using StorageFileApp.ConsoleApp.Services;
using System;

namespace StorageFileApp.ConsoleApp.Services;

public class HealthMonitoringService(
    ILogger<HealthMonitoringService> logger,
    IFileHealthUseCase fileHealthUseCase,
    MenuService menuService)
{
    private readonly ILogger<HealthMonitoringService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileHealthUseCase _fileHealthUseCase = fileHealthUseCase ?? throw new ArgumentNullException(nameof(fileHealthUseCase));
    private readonly MenuService _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));

    public async Task HandleHealthMonitoringAsync()
    {
        _logger.LogInformation("Health Monitoring Service started");

        while (true)
        {
            var choice = await _menuService.DisplayHealthMonitoringMenuAsync();

            switch (choice)
            {
                case "1":
                    await SystemHealthCheckAsync();
                    break;
                case "2":
                    await StorageProviderStatusAsync();
                    break;
                case "3":
                    await ChunkHealthStatusAsync();
                    break;
                case "4":
                    await SystemStatisticsAsync();
                    break;
                case "5":
                    return; // Back to main menu
                default:
                    await _menuService.DisplayMessageAsync("Invalid choice. Please try again.", true);
                    await _menuService.WaitForUserInputAsync();
                    break;
            }
        }
    }

    private async Task SystemHealthCheckAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    System Health Check                     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var request = new Application.DTOs.GetSystemHealthRequest(
                IncludeStorageProviders: true,
                IncludeDatabase: true,
                IncludeMessageQueue: true
            );

            var result = await _fileHealthUseCase.GetSystemHealthAsync(request);

            if (result.Success && result.HealthInfo != null)
            {
                var health = result.HealthInfo;
                var statusColor = result.IsHealthy ? ConsoleColor.Green : ConsoleColor.Red;
                var statusText = result.IsHealthy ? "HEALTHY" : "UNHEALTHY";

                Console.ForegroundColor = statusColor;
                Console.WriteLine($"System Status: {statusText}");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine($"Database: {(health.DatabaseHealthy ? "✓ Healthy" : "✗ Unhealthy")}");
                Console.WriteLine($"Message Queue: {(health.MessageQueueHealthy ? "✓ Healthy" : "✗ Unhealthy")}");
                Console.WriteLine($"Storage Providers: {(health.StorageProvidersHealthy ? "✓ Healthy" : "✗ Unhealthy")}");
                Console.WriteLine();

                Console.WriteLine("Storage Provider Summary:");
                Console.WriteLine($"  Total Providers: {health.TotalStorageProviders}");
                Console.WriteLine($"  Healthy: {health.HealthyStorageProviders}");
                Console.WriteLine($"  Unhealthy: {health.UnhealthyStorageProviders}");
                Console.WriteLine();

                Console.WriteLine($"Last Checked: {health.CheckedAt:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                await _menuService.DisplayMessageAsync($"Error checking system health: {result.ErrorMessage}", true);
            }

            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SystemHealthCheckAsync");
            await _menuService.DisplayMessageAsync($"Error checking system health: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task StorageProviderStatusAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                 Storage Provider Status                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var request = new Application.DTOs.GetStorageProviderHealthRequest(IncludeAll: true);
            var result = await _fileHealthUseCase.GetStorageProviderHealthAsync(request);

            if (result.Success && result.Providers != null)
            {
                Console.WriteLine("Storage Provider Health Status:");
                Console.WriteLine(new string('─', 80));
                Console.WriteLine($"{"Name",-30} {"Type",-15} {"Status",-10} {"Active",-8} {"Space",-15}");
                Console.WriteLine(new string('─', 80));

                foreach (var provider in result.Providers)
                {
                    var statusColor = provider.IsHealthy ? ConsoleColor.Green : ConsoleColor.Red;
                    var statusText = provider.IsHealthy ? "Healthy" : "Unhealthy";
                    var activeText = provider.IsActive ? "Yes" : "No";
                    var spaceText = provider.AvailableSpace.HasValue ? 
                        $"{provider.AvailableSpace.Value / (1024 * 1024):N0} MB" : "N/A";

                    Console.ForegroundColor = statusColor;
                    Console.WriteLine($"{provider.Name,-30} {provider.Type,-15} {statusText,-10} {activeText,-8} {spaceText,-15}");
                    Console.ResetColor();

                    if (!string.IsNullOrEmpty(provider.ErrorMessage))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Error: {provider.ErrorMessage}");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine(new string('─', 80));
                Console.WriteLine($"Total Providers: {result.Providers.Count}");
                Console.WriteLine($"Healthy: {result.Providers.Count(p => p.IsHealthy)}");
                Console.WriteLine($"Unhealthy: {result.Providers.Count(p => !p.IsHealthy)}");
            }
            else
            {
                await _menuService.DisplayMessageAsync($"Error checking storage provider status: {result.ErrorMessage}", true);
            }

            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StorageProviderStatusAsync");
            await _menuService.DisplayMessageAsync($"Error checking storage provider status: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task ChunkHealthStatusAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   Chunk Health Status                     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var fileId = await _menuService.GetUserInputAsync("Enter file ID to check chunk health (or press Enter for all): ");
            
            if (string.IsNullOrWhiteSpace(fileId))
            {
                // Show overall chunk health statistics
                await _menuService.DisplayMessageAsync("📊 Overall Chunk Health Statistics:", false);
                
                var statsRequest = new Application.DTOs.GetSystemStatisticsRequest();
                var statsResult = await _fileHealthUseCase.GetSystemStatisticsAsync(statsRequest);
                
                if (statsResult.Success && statsResult.Statistics != null)
                {
                    var stats = statsResult.Statistics;
                    Console.WriteLine($"\n🧩 Chunk Status Distribution:");
                    Console.WriteLine($"  Pending: {stats.ChunksByStatus_Pending:N0}");
                    Console.WriteLine($"  Processing: {stats.ChunksByStatus_Processing:N0}");
                    Console.WriteLine($"  Stored: {stats.ChunksByStatus_Stored:N0}");
                    Console.WriteLine($"  Failed: {stats.ChunksByStatus_Failed:N0}");
                    
                    var totalChunks = stats.ChunksByStatus_Pending + stats.ChunksByStatus_Processing + 
                                    stats.ChunksByStatus_Stored + stats.ChunksByStatus_Failed;
                    var healthyPercentage = totalChunks > 0 ? (stats.ChunksByStatus_Stored * 100.0 / totalChunks) : 0;
                    
                    Console.WriteLine($"\n✅ Chunk Health: {healthyPercentage:F1}% healthy");
                }
            }
            else
            {
                // Check specific file's chunk health
                if (!Guid.TryParse(fileId, out var fileGuid))
                {
                    await _menuService.DisplayMessageAsync("Invalid file ID format.", true);
                    await _menuService.WaitForUserInputAsync();
                    return;
                }
                
                Console.WriteLine($"\n🔍 Checking chunk health for file: {fileGuid}");
                
                // This would need to be implemented in the health use case
                // For now, we'll show a placeholder
                await _menuService.DisplayMessageAsync("Detailed chunk health validation would be performed here.", false);
                await _menuService.DisplayMessageAsync("This includes checksum validation, storage provider availability, and data integrity checks.", false);
            }
            
            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChunkHealthStatusAsync");
            await _menuService.DisplayMessageAsync($"Error checking chunk health: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task SystemStatisticsAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    System Statistics                      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var request = new Application.DTOs.GetSystemStatisticsRequest();
            var result = await _fileHealthUseCase.GetSystemStatisticsAsync(request);

            if (result.Success && result.Statistics != null)
            {
                var stats = result.Statistics;

                Console.WriteLine("📊 System Overview:");
                Console.WriteLine($"  Total Files: {stats.TotalFiles:N0}");
                Console.WriteLine($"  Total Chunks: {stats.TotalChunks:N0}");
                Console.WriteLine($"  Total Storage Used: {stats.TotalStorageUsed / (1024 * 1024):N0} MB");
                Console.WriteLine();

                Console.WriteLine("📁 File Status Distribution:");
                Console.WriteLine($"  Pending: {stats.FilesByStatus_Pending:N0}");
                Console.WriteLine($"  Processing: {stats.FilesByStatus_Processing:N0}");
                Console.WriteLine($"  Available: {stats.FilesByStatus_Available:N0}");
                Console.WriteLine($"  Failed: {stats.FilesByStatus_Failed:N0}");
                Console.WriteLine();

                Console.WriteLine("🧩 Chunk Status Distribution:");
                Console.WriteLine($"  Pending: {stats.ChunksByStatus_Pending:N0}");
                Console.WriteLine($"  Processing: {stats.ChunksByStatus_Processing:N0}");
                Console.WriteLine($"  Stored: {stats.ChunksByStatus_Stored:N0}");
                Console.WriteLine($"  Failed: {stats.ChunksByStatus_Failed:N0}");
                Console.WriteLine();

                Console.WriteLine($"📅 Generated: {stats.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                await _menuService.DisplayMessageAsync($"Error retrieving system statistics: {result.ErrorMessage}", true);
            }

            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SystemStatisticsAsync");
            await _menuService.DisplayMessageAsync($"Error retrieving system statistics: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }
}
