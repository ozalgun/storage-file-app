namespace StorageFileApp.Domain.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents);
}
