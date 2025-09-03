using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StorageFileApp.Application.DependencyInjection;
using StorageFileApp.ConsoleApp.Services;
using StorageFileApp.Infrastructure.DependencyInjection;
using StorageFileApp.Infrastructure.Data;

namespace StorageFileApp.ConsoleApp;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            Log.Information("Starting Storage File App Console Application");

            // Build host
            var host = CreateHostBuilder(args, config).Build();

            // Seed data
            Log.Information("Seeding database...");
            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<StorageFileDbContext>();
                await SeedData.SeedAsync(context);
            }
            Log.Information("Database seeding completed");

            // Run the application
            var app = host.Services.GetRequiredService<ConsoleApplicationService>();
            await app.RunAsync();

            Log.Information("Storage File App Console Application completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Storage File App Console Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Add configuration
                services.AddSingleton(configuration);

                // Add application services
                services.AddApplicationServices();

                // Add infrastructure services
                services.AddInfrastructureServices(configuration);

                // Add console services
                services.AddScoped<ConsoleApplicationService>();
                services.AddScoped<FileOperationService>();
                services.AddScoped<ChunkingOperationService>();
                services.AddScoped<HealthMonitoringService>();
                services.AddScoped<MenuService>();
            });
    }
}