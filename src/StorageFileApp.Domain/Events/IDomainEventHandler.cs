namespace StorageFileApp.Domain.Events;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent);
}

public interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent);
}
