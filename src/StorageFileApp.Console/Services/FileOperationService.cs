using Microsoft.Extensions.Logging;
using StorageFileApp.Application.UseCases;
using StorageFileApp.ConsoleApp.Services;
using System;
using StorageFileApp.Application.DTOs;
using StorageFileApp.Domain.Services;
using System.Security.Cryptography;
using Console = System.Console;

namespace StorageFileApp.ConsoleApp.Services;

public class FileOperationService(
    ILogger<FileOperationService> logger,
    IFileStorageUseCase fileStorageUseCase,
    MenuService menuService,
    IFileIntegrityDomainService integrityService)
{
    private readonly ILogger<FileOperationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileStorageUseCase _fileStorageUseCase = fileStorageUseCase ?? throw new ArgumentNullException(nameof(fileStorageUseCase));
    private readonly MenuService _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
    private readonly IFileIntegrityDomainService _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));

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
                Console.WriteLine("\nStoring file...");
                
                var contentType = GetContentType(fileName);
                
                // Use memory-efficient approach for large files
                byte[]? fileBytes = null;
                
                if (fileSize >= 100 * 1024 * 1024) // 100MB threshold
                {
                    Console.WriteLine("Large file detected. Using memory-efficient processing...");
                    
                    // For large files, we'll use streaming in the application service
                    fileBytes = null; // Will be handled by streaming service
                }
                else
                {
                    // For small files, use traditional approach
                    fileBytes = await File.ReadAllBytesAsync(filePath);
                }
                
                var request = new StoreFileRequest(
                    FileName: fileName,
                    FileSize: fileSize,
                    ContentType: contentType,
                    Description: $"Stored from: {filePath}",
                    CustomProperties: null // Checksum will be calculated in Application layer
                );
                
                var result = await _fileStorageUseCase.StoreFileAsync(request, fileBytes ?? Array.Empty<byte>(), filePath);
                
                if (result.Success)
                {
                    await _menuService.DisplayMessageAsync($"✅ File stored successfully! File ID: {result.FileId}", false);
                    if (result.Warnings?.Any() == true)
                    {
                        Console.WriteLine("\n⚠️ Warnings:");
                        foreach (var warning in result.Warnings)
                        {
                            Console.WriteLine($"  - {warning}");
                        }
                    }
                }
                else
                {
                    await _menuService.DisplayMessageAsync($"❌ Failed to store file: {result.ErrorMessage}", true);
                }
                
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
                Console.WriteLine("\nRetrieving file...");
                
                if (!Guid.TryParse(fileId, out var fileGuid))
                {
                    await _menuService.DisplayMessageAsync("Invalid file ID format.", true);
                    await _menuService.WaitForUserInputAsync();
                    return;
                }
                
                var request = new RetrieveFileRequest(
                    FileId: fileGuid,
                    OutputPath: outputPath
                );
                
                var result = await _fileStorageUseCase.RetrieveFileAsync(request);
                
                if (result.Success)
                {
                    await _menuService.DisplayMessageAsync($"✅ File retrieved successfully! Saved to: {result.FilePath}", false);
                    
                    if (result.FileMetadata != null)
                    {
                        Console.WriteLine("\n📄 File Metadata:");
                        Console.WriteLine($"  Content Type: {result.FileMetadata.ContentType}");
                        if (!string.IsNullOrEmpty(result.FileMetadata.Description))
                            Console.WriteLine($"  Description: {result.FileMetadata.Description}");
                        if (result.FileMetadata.CustomProperties?.Any() == true)
                        {
                            Console.WriteLine("  Custom Properties:");
                            foreach (var prop in result.FileMetadata.CustomProperties)
                            {
                                Console.WriteLine($"    {prop.Key}: {prop.Value}");
                            }
                        }
                    }
                }
                else
                {
                    await _menuService.DisplayMessageAsync($"❌ Failed to retrieve file: {result.ErrorMessage}", true);
                }
                
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
                Console.WriteLine("\nDeleting file...");
                
                if (!Guid.TryParse(fileId, out var fileGuid))
                {
                    await _menuService.DisplayMessageAsync("Invalid file ID format.", true);
                    await _menuService.WaitForUserInputAsync();
                    return;
                }
                
                var request = new DeleteFileRequest(
                    FileId: fileGuid,
                    ForceDelete: false
                );
                
                var result = await _fileStorageUseCase.DeleteFileAsync(request);
                
                if (result.Success)
                {
                    await _menuService.DisplayMessageAsync("✅ File deleted successfully!", false);
                }
                else
                {
                    await _menuService.DisplayMessageAsync($"❌ Failed to delete file: {result.ErrorMessage}", true);
                }
                
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
            var pageNumber = 1;
            var pageSize = 20;
            
            while (true)
            {
                Console.WriteLine($"\n📋 Files (Page {pageNumber}, {pageSize} per page):");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                
                var request = new ListFilesRequest(
                    PageNumber: pageNumber,
                    PageSize: pageSize
                );
                
                var result = await _fileStorageUseCase.ListFilesAsync(request);
                
                if (result.Success && result.Files?.Any() == true)
                {
                    Console.WriteLine($"{"ID",-36} {"Name",-30} {"Size",-12} {"Status",-12} {"Created",-20}");
                    Console.WriteLine(new string('-', 110));
                    
                    foreach (var file in result.Files)
                    {
                        var sizeFormatted = FormatFileSize(file.Size);
                        var statusFormatted = file.Status.ToString();
                        var createdFormatted = file.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                        
                        Console.WriteLine($"{file.Id,-36} {TruncateString(file.Name, 30),-30} {sizeFormatted,-12} {statusFormatted,-12} {createdFormatted,-20}");
                    }
                    
                    Console.WriteLine(new string('-', 110));
                    Console.WriteLine($"Total: {result.TotalCount} files");
                    
                    if (result.TotalCount > pageSize)
                    {
                        Console.WriteLine("\nNavigation:");
                        Console.WriteLine("  [N] Next page  [P] Previous page  [S] Change page size  [Q] Quit");
                        
                        var navChoice = await _menuService.GetUserInputAsync("Choice: ").ConfigureAwait(false);
                        
                        switch (navChoice?.ToUpper())
                        {
                            case "N":
                                if (pageNumber * pageSize < result.TotalCount)
                                    pageNumber++;
                                break;
                            case "P":
                                if (pageNumber > 1)
                                    pageNumber--;
                                break;
                            case "S":
                                var newPageSize = await _menuService.GetUserInputAsync("Enter page size (5-100): ");
                                if (int.TryParse(newPageSize, out var size) && size >= 5 && size <= 100)
                                {
                                    pageSize = size;
                                    pageNumber = 1;
                                }
                                break;
                            case "Q":
                                return;
                            default:
                                await _menuService.DisplayMessageAsync("Invalid choice.", true);
                                await _menuService.WaitForUserInputAsync();
                                break;
                        }
                    }
                    else
                    {
                        await _menuService.WaitForUserInputAsync();
                        return;
                    }
                }
                else
                {
                    await _menuService.DisplayMessageAsync("No files found.", false);
                    await _menuService.WaitForUserInputAsync();
                    return;
                }
            }
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

            Console.WriteLine("\n🔍 Searching files...");
            
            var request = new ListFilesRequest(
                PageNumber: 1,
                PageSize: 50,
                SearchTerm: searchTerm
            );
            
            var result = await _fileStorageUseCase.ListFilesAsync(request);
            
            if (result.Success && result.Files?.Any() == true)
            {
                Console.WriteLine($"\n📋 Search Results for '{searchTerm}' ({result.Files.Count} files found):");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine($"{"ID",-36} {"Name",-30} {"Size",-12} {"Status",-12} {"Created",-20}");
                Console.WriteLine(new string('-', 110));
                
                foreach (var file in result.Files)
                {
                    var sizeFormatted = FormatFileSize(file.Size);
                    var statusFormatted = file.Status.ToString();
                    var createdFormatted = file.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    
                    Console.WriteLine($"{file.Id,-36} {TruncateString(file.Name, 30),-30} {sizeFormatted,-12} {statusFormatted,-12} {createdFormatted,-20}");
                }
                
                Console.WriteLine(new string('-', 110));
                Console.WriteLine($"Total: {result.TotalCount} files found");
            }
            else
            {
                await _menuService.DisplayMessageAsync($"No files found matching '{searchTerm}'.", false);
            }
            
            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchFilesAsync");
            await _menuService.DisplayMessageAsync($"Error searching files: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }
    
    
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".zip" => "application/zip",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
    
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;
        return input.Substring(0, maxLength - 3) + "...";
    }
}
