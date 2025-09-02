using Microsoft.Extensions.Logging;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.DTOs;
using StorageFileApp.ConsoleApp.Services;
using System;

namespace StorageFileApp.ConsoleApp.Services;

public class ChunkingOperationService(
    ILogger<ChunkingOperationService> logger,
    IFileChunkingUseCase fileChunkingUseCase,
    MenuService menuService)
{
    private readonly ILogger<ChunkingOperationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileChunkingUseCase _fileChunkingUseCase = fileChunkingUseCase ?? throw new ArgumentNullException(nameof(fileChunkingUseCase));
    private readonly MenuService _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));

    public async Task HandleChunkingOperationsAsync()
    {
        _logger.LogInformation("Chunking Operations Service started");

        while (true)
        {
            var choice = await _menuService.DisplayChunkingOperationsMenuAsync();

            switch (choice)
            {
                case "1":
                    await ChunkFileAsync();
                    break;
                case "2":
                    await MergeChunksAsync();
                    break;
                case "3":
                    await ViewChunkInformationAsync();
                    break;
                case "4":
                    await ReplicateChunksAsync();
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

    private async Task ChunkFileAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                       Chunk File                           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var fileId = await _menuService.GetUserInputAsync("Enter file ID to chunk: ");
            
            if (string.IsNullOrWhiteSpace(fileId))
            {
                await _menuService.DisplayMessageAsync("File ID is required.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            if (await _menuService.ConfirmOperationAsync("chunk this file"))
            {
                Console.WriteLine("\nChunking file...");
                
                if (!Guid.TryParse(fileId, out var fileGuid))
                {
                    await _menuService.DisplayMessageAsync("Invalid file ID format.", true);
                    await _menuService.WaitForUserInputAsync();
                    return;
                }
                
                var request = new ChunkFileRequest(
                    FileId: fileGuid,
                    ChunkSize: null, // Use default chunk size
                    Strategy: null // Use default strategy
                );
                
                var result = await _fileChunkingUseCase.ChunkFileAsync(request);
                
                if (result.Success)
                {
                    await _menuService.DisplayMessageAsync($"✅ File chunked successfully! Created {result.Chunks?.Count ?? 0} chunks.", false);
                    
                    if (result.Chunks?.Any() == true)
                    {
                        Console.WriteLine("\n📦 Chunk Details:");
                        Console.WriteLine($"{"Order",-6} {"Size",-12} {"Status",-12} {"Storage Provider",-36} {"Created",-20}");
                        Console.WriteLine(new string('-', 86));
                        
                        foreach (var chunk in result.Chunks.OrderBy(c => c.Order))
                        {
                            var sizeFormatted = FormatFileSize(chunk.Size);
                            var createdFormatted = chunk.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                            
                            Console.WriteLine($"{chunk.Order,-6} {sizeFormatted,-12} {chunk.Status,-12} {chunk.StorageProviderId,-36} {createdFormatted,-20}");
                        }
                    }
                    
                    if (result.ProcessingTime.HasValue)
                    {
                        Console.WriteLine($"\n⏱️ Processing time: {result.ProcessingTime.Value.TotalSeconds:F2} seconds");
                    }
                }
                else
                {
                    await _menuService.DisplayMessageAsync($"❌ Failed to chunk file: {result.ErrorMessage}", true);
                }
                
                await _menuService.WaitForUserInputAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChunkFileAsync");
            await _menuService.DisplayMessageAsync($"Error chunking file: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task MergeChunksAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      Merge Chunks                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var fileId = await _menuService.GetUserInputAsync("Enter file ID to merge chunks: ");
            var outputPath = await _menuService.GetUserInputAsync("Enter output path: ");
            
            if (string.IsNullOrWhiteSpace(fileId) || string.IsNullOrWhiteSpace(outputPath))
            {
                await _menuService.DisplayMessageAsync("File ID and output path are required.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            if (await _menuService.ConfirmOperationAsync("merge chunks for this file"))
            {
                Console.WriteLine("\nMerging chunks...");
                
                if (!Guid.TryParse(fileId, out var fileGuid))
                {
                    await _menuService.DisplayMessageAsync("Invalid file ID format.", true);
                    await _menuService.WaitForUserInputAsync();
                    return;
                }
                
                var request = new MergeChunksRequest(
                    FileId: fileGuid,
                    OutputPath: outputPath,
                    ValidateIntegrity: true
                );
                
                var result = await _fileChunkingUseCase.MergeChunksAsync(request);
                
                if (result.Success)
                {
                    await _menuService.DisplayMessageAsync($"✅ Chunks merged successfully! Output: {result.OutputPath}", false);
                    
                    if (result.IntegrityValid.HasValue)
                    {
                        var integrityStatus = result.IntegrityValid.Value ? "✅ Valid" : "❌ Invalid";
                        Console.WriteLine($"🔍 Integrity check: {integrityStatus}");
                    }
                    
                    if (result.ProcessingTime.HasValue)
                    {
                        Console.WriteLine($"⏱️ Processing time: {result.ProcessingTime.Value.TotalSeconds:F2} seconds");
                    }
                }
                else
                {
                    await _menuService.DisplayMessageAsync($"❌ Failed to merge chunks: {result.ErrorMessage}", true);
                }
                
                await _menuService.WaitForUserInputAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MergeChunksAsync");
            await _menuService.DisplayMessageAsync($"Error merging chunks: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task ViewChunkInformationAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   Chunk Information                        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var fileId = await _menuService.GetUserInputAsync("Enter file ID to view chunk information: ");
            
            if (string.IsNullOrWhiteSpace(fileId))
            {
                await _menuService.DisplayMessageAsync("File ID is required.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            Console.WriteLine("\nRetrieving chunk information...");
            
            if (!Guid.TryParse(fileId, out var fileGuid))
            {
                await _menuService.DisplayMessageAsync("Invalid file ID format.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }
            
            // First, let's get the file status to see if it has chunks
            var fileStatusRequest = new GetFileStatusRequest(fileGuid);
            var fileStatusResult = await _fileChunkingUseCase.GetFileStatusAsync(fileStatusRequest);
            
            if (!fileStatusResult.Success)
            {
                await _menuService.DisplayMessageAsync($"Failed to get file status: {fileStatusResult.ErrorMessage}", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }
            
            Console.WriteLine($"\n📄 File Status: {fileStatusResult.Status}");
            
            if (fileStatusResult.AdditionalInfo?.ContainsKey("ChunkCount") == true)
            {
                var chunkCount = fileStatusResult.AdditionalInfo["ChunkCount"];
                Console.WriteLine($"📦 Total Chunks: {chunkCount}");
                
                if (fileStatusResult.AdditionalInfo.ContainsKey("ChunkDetails"))
                {
                    Console.WriteLine("\n📋 Chunk Details:");
                    Console.WriteLine($"{"Order",-6} {"Size",-12} {"Status",-12} {"Storage Provider",-36}");
                    Console.WriteLine(new string('-', 66));
                    
                    // This would need to be implemented in the use case to return detailed chunk info
                    Console.WriteLine("Detailed chunk information would be displayed here.");
                }
            }
            else
            {
                Console.WriteLine("No chunk information available for this file.");
            }
            
            await _menuService.WaitForUserInputAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ViewChunkInformationAsync");
            await _menuService.DisplayMessageAsync($"Error viewing chunk information: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
    }

    private async Task ReplicateChunksAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    Replicate Chunks                        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        try
        {
            var fileId = await _menuService.GetUserInputAsync("Enter file ID to replicate chunks: ");
            
            if (string.IsNullOrWhiteSpace(fileId))
            {
                await _menuService.DisplayMessageAsync("File ID is required.", true);
                await _menuService.WaitForUserInputAsync();
                return;
            }

            if (await _menuService.ConfirmOperationAsync("replicate chunks for this file"))
            {
                Console.WriteLine("\nReplicating chunks...");
                
                if (!Guid.TryParse(fileId, out var fileGuid))
                {
                    await _menuService.DisplayMessageAsync("Invalid file ID format.", true);
                    await _menuService.WaitForUserInputAsync();
                    return;
                }
                
                // This would need to be implemented in the health use case
                // For now, we'll show a placeholder message
                await _menuService.DisplayMessageAsync("Chunk replication functionality is available through the Health Monitoring menu.", false);
                await _menuService.DisplayMessageAsync("This feature ensures data redundancy across multiple storage providers.", false);
                
                await _menuService.WaitForUserInputAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReplicateChunksAsync");
            await _menuService.DisplayMessageAsync($"Error replicating chunks: {ex.Message}", true);
            await _menuService.WaitForUserInputAsync();
        }
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
}
