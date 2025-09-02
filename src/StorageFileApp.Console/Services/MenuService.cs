using Microsoft.Extensions.Logging;
using System;

namespace StorageFileApp.ConsoleApp.Services;

public class MenuService(ILogger<MenuService> logger)
{
    private readonly ILogger<MenuService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<string> DisplayMainMenuAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                        Main Menu                           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Please select an option:");
        Console.WriteLine();
        Console.WriteLine("1. 📁 File Operations");
        Console.WriteLine("   • Store files");
        Console.WriteLine("   • Retrieve files");
        Console.WriteLine("   • Delete files");
        Console.WriteLine("   • List files");
        Console.WriteLine();
        Console.WriteLine("2. 🔧 Chunking Operations");
        Console.WriteLine("   • Chunk files");
        Console.WriteLine("   • Merge chunks");
        Console.WriteLine("   • View chunk information");
        Console.WriteLine();
        Console.WriteLine("3. 🏥 Health Monitoring");
        Console.WriteLine("   • Check system health");
        Console.WriteLine("   • View storage provider status");
        Console.WriteLine("   • Monitor chunk health");
        Console.WriteLine();
        Console.WriteLine("4. ℹ️  System Information");
        Console.WriteLine();
        Console.WriteLine("5. 🚪 Exit");
        Console.WriteLine();
        Console.Write("Enter your choice (1-5): ");

        var choice = Console.ReadLine() ?? string.Empty;
        _logger.LogDebug("User selected menu option: {Choice}", choice);
        
        return Task.FromResult(choice);
    }

    public Task<string> DisplayFileOperationsMenuAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    File Operations                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Please select a file operation:");
        Console.WriteLine();
        Console.WriteLine("1. 📤 Store File");
        Console.WriteLine("2. 📥 Retrieve File");
        Console.WriteLine("3. 🗑️  Delete File");
        Console.WriteLine("4. 📋 List Files");
        Console.WriteLine("5. 🔍 Search Files");
        Console.WriteLine("6. ⬅️  Back to Main Menu");
        Console.WriteLine();
        Console.Write("Enter your choice (1-6): ");

        var choice = Console.ReadLine() ?? string.Empty;
        _logger.LogDebug("User selected file operation: {Choice}", choice);
        
        return Task.FromResult(choice);
    }

    public Task<string> DisplayChunkingOperationsMenuAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  Chunking Operations                       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Please select a chunking operation:");
        Console.WriteLine();
        Console.WriteLine("1. ✂️  Chunk File");
        Console.WriteLine("2. 🔗 Merge Chunks");
        Console.WriteLine("3. 📊 View Chunk Information");
        Console.WriteLine("4. 🔄 Replicate Chunks");
        Console.WriteLine("5. ⬅️  Back to Main Menu");
        Console.WriteLine();
        Console.Write("Enter your choice (1-5): ");

        var choice = Console.ReadLine() ?? string.Empty;
        _logger.LogDebug("User selected chunking operation: {Choice}", choice);
        
        return Task.FromResult(choice);
    }

    public Task<string> DisplayHealthMonitoringMenuAsync()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  Health Monitoring                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Please select a health monitoring option:");
        Console.WriteLine();
        Console.WriteLine("1. 🏥 System Health Check");
        Console.WriteLine("2. 💾 Storage Provider Status");
        Console.WriteLine("3. 📊 Chunk Health Status");
        Console.WriteLine("4. 📈 System Statistics");
        Console.WriteLine("5. ⬅️  Back to Main Menu");
        Console.WriteLine();
        Console.Write("Enter your choice (1-5): ");

        var choice = Console.ReadLine() ?? string.Empty;
        _logger.LogDebug("User selected health monitoring option: {Choice}", choice);
        
        return Task.FromResult(choice);
    }

    public Task<bool> ConfirmOperationAsync(string operation)
    {
        Console.WriteLine();
        Console.Write($"Are you sure you want to {operation}? (y/N): ");
        var response = Console.ReadLine() ?? string.Empty;
        return Task.FromResult(response.ToLowerInvariant() == "y" || response.ToLowerInvariant() == "yes");
    }

    public Task<string> GetUserInputAsync(string prompt)
    {
        Console.Write(prompt);
        return Task.FromResult(Console.ReadLine() ?? string.Empty);
    }

    public Task DisplayMessageAsync(string message, bool isError = false)
    {
        var prefix = isError ? "❌" : "✅";
        var color = isError ? ConsoleColor.Red : ConsoleColor.Green;
        
        Console.ForegroundColor = color;
        Console.WriteLine($"{prefix} {message}");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    public Task WaitForUserInputAsync(string message = "Press any key to continue...")
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.ReadKey();
        return Task.CompletedTask;
    }
}
