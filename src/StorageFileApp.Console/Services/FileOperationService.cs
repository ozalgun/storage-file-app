using Microsoft.Extensions.Logging;
using StorageFileApp.Application.UseCases;
using StorageFileApp.ConsoleApp.Services;
using System;

namespace StorageFileApp.ConsoleApp.Services;

public class FileOperationService(
    ILogger<FileOperationService> logger,
    IFileStorageUseCase fileStorageUseCase,
    MenuService menuService)
{
    private readonly ILogger<FileOperationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileStorageUseCase _fileStorageUseCase = fileStorageUseCase ?? throw new ArgumentNullException(nameof(fileStorageUseCase));
    private readonly MenuService _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));

    public async Task HandleFileOperationsAsync()
    {
        _logger.LogInformation("File Operations Service started");

        while (true)
        {
            var choice = await _menuService.DisplayFileOperationsMenuAsync();

            switch (choice)
            {
                case "1":
                    await StoreFileAsync();
                    break;
                case "2":
                    await RetrieveFileAsync();
                    break;
                case "3":
                    await DeleteFileAsync();
                    break;
                case "4":
                    await ListFilesAsync();
                    break;
                case "5":
                    await SearchFilesAsync();
                    break;
                case "6":
                    return; // Back to main menu
                default:
                    await _menuService.DisplayMessageAsync("Invalid choice. Please try again.", true);
                    await _menuService.WaitForUserInputAsync();
                    break;
            }
        }
    }

    private async Task StoreFileAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                        Store File                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var filePath = await _menuService.GetUserInputAsync("Enter file path: ");
            
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                await _menuService.DisplayMessageAsync("File not found or path is empty.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;
            
            Console.WriteLine($"\nFile Information:");
            Console.WriteLine($"Name: {fileName}");
            Console.WriteLine($"Size: {fileSize:N0} bytes");
            Console.WriteLine($"Path: {filePath}");
            
            if (await _menuService.ConfirmOperationAsync("store this file"))
            {
                // TODO: Implement file storage
                await _menuService.DisplayMessageAsync("File storage functionality will be implemented here.");
                await _menuService.WaitForUserInputAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StoreFileAsync");
            await _menuService.DisplayMessageAsync($"Error storing file: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task RetrieveFileAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      Retrieve File                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var fileId = await _menuService.GetUserInputAsync("Enter file ID: ");
            var outputPath = await _menuService.GetUserInputAsync("Enter output path: ");
            
            if (string.IsNullOrWhiteSpace(fileId) || string.IsNullOrWhiteSpace(outputPath))
            {
                await _menuService.DisplayMessageAsync("File ID and output path are required.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            if (await _menuService.ConfirmOperationAsync("retrieve this file"))
            {
                // TODO: Implement file retrieval
                await _menuService.DisplayMessageAsync("File retrieval functionality will be implemented here.");
                await _menuService.WaitForUserInputAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RetrieveFileAsync");
            await _menuService.DisplayMessageAsync($"Error retrieving file: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task DeleteFileAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                       Delete File                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var fileId = await _menuService.GetUserInputAsync("Enter file ID to delete: ");
            
            if (string.IsNullOrWhiteSpace(fileId))
            {
                await _menuService.DisplayMessageAsync("File ID is required.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            if (await _menuService.ConfirmOperationAsync("delete this file"))
            {
                // TODO: Implement file deletion
                await _menuService.DisplayMessageAsync("File deletion functionality will be implemented here.");
                await _menuService.WaitForUserInputAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteFileAsync");
            await _menuService.DisplayMessageAsync($"Error deleting file: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task ListFilesAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                       List Files                           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            // TODO: Implement file listing
            await _menuService.DisplayMessageAsync("File listing functionality will be implemented here.");
            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ListFilesAsync");
            await _menuService.DisplayMessageAsync($"Error listing files: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task SearchFilesAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      Search Files                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var searchTerm = await _menuService.GetUserInputAsync("Enter search term: ");
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                await _menuService.DisplayMessageAsync("Search term is required.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            // TODO: Implement file search
            await _menuService.DisplayMessageAsync("File search functionality will be implemented here.");
            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchFilesAsync");
            await _menuService.DisplayMessageAsync($"Error searching files: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }
}
