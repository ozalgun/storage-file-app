using Microsoft.Extensions.Logging;
using StorageFileApp.ConsoleApp.Services;
using System;
using Console = System.Console;

namespace StorageFileApp.ConsoleApp.Services;

public class ConsoleApplicationService(
    ILogger<ConsoleApplicationService> logger,
    MenuService menuService,
    FileOperationService fileOperationService,
    ChunkingOperationService chunkingOperationService,
    HealthMonitoringService healthMonitoringService)
{
    private readonly ILogger<ConsoleApplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly MenuService _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
    private readonly FileOperationService _fileOperationService = fileOperationService ?? throw new ArgumentNullException(nameof(fileOperationService));
    private readonly ChunkingOperationService _chunkingOperationService = chunkingOperationService ?? throw new ArgumentNullException(nameof(chunkingOperationService));
    private readonly HealthMonitoringService _healthMonitoringService = healthMonitoringService ?? throw new ArgumentNullException(nameof(healthMonitoringService));

    public async Task RunAsync()
    {
        _logger.LogInformation("Console Application Service started");

        try
        {
            await DisplayWelcomeMessageAsync();
            await RunMainMenuAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Console Application Service");
            Console.WriteLine($"\n❌ An error occurred: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            _logger.LogInformation("Console Application Service completed");
        }
    }

    private Task DisplayWelcomeMessageAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    Storage File App                         ║");
        Console.WriteLine("║              Distributed File Storage System                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Welcome to the Storage File App!");
        Console.WriteLine("This application provides distributed file storage with chunking capabilities.");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("• File storage with automatic chunking");
        Console.WriteLine("• Distributed storage across multiple providers");
        Console.WriteLine("• File integrity validation with SHA256 checksums");
        Console.WriteLine("• Health monitoring and replication");
        Console.WriteLine("• Clean Architecture with Domain-Driven Design");
        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        return Task.CompletedTask;
    }

    private async Task RunMainMenuAsync()
    {
        while (true)
        {
            Console.Clear();
            var choice = await _menuService.DisplayMainMenuAsync();

            switch (choice)
            {
                case "1":
                    await _fileOperationService.HandleFileOperationsAsync();
                    break;
                case "2":
                    await _chunkingOperationService.HandleChunkingOperationsAsync();
                    break;
                case "3":
                    await _healthMonitoringService.HandleHealthMonitoringAsync();
                    break;
                case "4":
                    await DisplaySystemInfoAsync();
                    break;
                case "5":
                    await DisplayRabbitMQInfoAsync();
                    break;
                case "6":
                    Console.WriteLine("\n👋 Thank you for using Storage File App!");
                    return;
                default:
                    Console.WriteLine("\n❌ Invalid choice. Please try again.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private Task DisplaySystemInfoAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      System Information                     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Application Version: 1.0.0");
        Console.WriteLine($".NET Version: {Environment.Version}");
        Console.WriteLine($"Operating System: {Environment.OSVersion}");
        Console.WriteLine($"Machine Name: {Environment.MachineName}");
        Console.WriteLine($"User Name: {Environment.UserName}");
        Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
        Console.WriteLine();
        Console.WriteLine("Architecture:");
        Console.WriteLine("• Domain Layer: Business logic and entities");
        Console.WriteLine("• Application Layer: Use cases and application services");
        Console.WriteLine("• Infrastructure Layer: Data access and external services");
        Console.WriteLine("• Console Layer: User interface and orchestration");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to main menu...");
        Console.ReadKey();
        return Task.CompletedTask;
    }

    private Task DisplayRabbitMQInfoAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    RabbitMQ Information                     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("🐰 RabbitMQ Configuration:");
        Console.WriteLine("• Host: localhost:5672");
        Console.WriteLine("• Management UI: http://localhost:15672");
        Console.WriteLine("• Username: storageuser");
        Console.WriteLine("• Password: storagepass123");
        Console.WriteLine("• Virtual Host: storage-vhost");
        Console.WriteLine();
        Console.WriteLine("📨 Configured Queues:");
        Console.WriteLine("• file-created-queue");
        Console.WriteLine("• file-status-changed-queue");
        Console.WriteLine("• file-deleted-queue");
        Console.WriteLine("• chunk-created-queue");
        Console.WriteLine("• chunk-status-changed-queue");
        Console.WriteLine("• chunk-stored-queue");
        Console.WriteLine("• storage-health-check-queue");
        Console.WriteLine("• storage-space-warning-queue");
        Console.WriteLine();
        Console.WriteLine("🔄 Event Flow:");
        Console.WriteLine("1. File Upload → FileCreatedEvent → file-created-queue");
        Console.WriteLine("2. Chunking → ChunkCreatedEvent → chunk-created-queue");
        Console.WriteLine("3. Storage → ChunkStoredEvent → chunk-stored-queue");
        Console.WriteLine("4. Status Update → FileStatusChangedEvent → file-status-changed-queue");
        Console.WriteLine();
        Console.WriteLine("💡 To test RabbitMQ:");
        Console.WriteLine("1. Start Docker Compose: docker-compose up -d");
        Console.WriteLine("2. Open Management UI: http://localhost:15672");
        Console.WriteLine("3. Login with: storageuser / storagepass123");
        Console.WriteLine("4. Check 'storage-vhost' virtual host");
        Console.WriteLine("5. Run the application to see queues created automatically");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to main menu...");
        Console.ReadKey();
        return Task.CompletedTask;
    }
}
