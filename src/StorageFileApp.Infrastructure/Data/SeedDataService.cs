using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StorageFileApp.Infrastructure.Data;

public class SeedDataService(IServiceProvider serviceProvider, ILogger<SeedDataService> logger)
    : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<SeedDataService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting seed data service...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageFileDbContext>();

        try
        {
            await SeedData.SeedAsync(context);
            _logger.LogInformation("Seed data completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding data");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seed data service stopped");
        return Task.CompletedTask;
    }
}
