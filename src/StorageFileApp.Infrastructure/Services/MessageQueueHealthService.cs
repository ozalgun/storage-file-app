using MassTransit;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;

namespace StorageFileApp.Infrastructure.Services;

public class MessageQueueHealthService(
    IBusControl busControl,
    ILogger<MessageQueueHealthService> logger)
    : IMessageQueueHealthService
{
    private readonly IBusControl _busControl = busControl ?? throw new ArgumentNullException(nameof(busControl));
    private readonly ILogger<MessageQueueHealthService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<bool> IsHealthyAsync()
    {
        try
        {
            var healthInfo = GetHealthInfoAsync().Result;
            return Task.FromResult(healthInfo.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking message queue health");
            return Task.FromResult(false);
        }
    }

    public Task<MessageQueueHealthInfo> GetHealthInfoAsync()
    {
        try
        {
            _logger.LogDebug("Checking message queue health...");

            // Check if the bus is started and connected
            if (_busControl == null)
            {
                return Task.FromResult(new MessageQueueHealthInfo(false, "Bus control is not available"));
            }

            // Try to get the bus state
            var busState = _busControl.Address;
            
            // In a real implementation, you might want to send a test message
            // or check the connection status more thoroughly
            var isHealthy = busState != null;

            _logger.LogDebug("Message queue health check completed. Healthy: {IsHealthy}", isHealthy);

            return Task.FromResult(new MessageQueueHealthInfo(isHealthy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking message queue health");
            return Task.FromResult(new MessageQueueHealthInfo(false, ex.Message));
        }
    }
}
