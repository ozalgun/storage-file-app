namespace StorageFileApp.Application.Interfaces;

public interface IMessageQueueHealthService
{
    Task<bool> IsHealthyAsync();
    Task<MessageQueueHealthInfo> GetHealthInfoAsync();
}

public record MessageQueueHealthInfo(
    bool IsHealthy,
    string? ErrorMessage = null,
    DateTime CheckedAt = default
)
{
    public DateTime CheckedAt { get; init; } = CheckedAt == default ? DateTime.UtcNow : CheckedAt;
}
