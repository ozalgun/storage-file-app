using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StorageFileApp.Domain.Events;
using System.Collections.Concurrent;

namespace StorageFileApp.Infrastructure.Events;

public class DomainEventPublisher(IServiceProvider serviceProvider, ILogger<DomainEventPublisher> logger)
    : IDomainEventPublisher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<DomainEventPublisher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly ConcurrentDictionary<Type, Type[]> HandlerTypesCache = new();

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent);
        _logger.LogInformation("Publishing domain event: {EventType} at {Timestamp}", 
            eventType.Name, DateTime.UtcNow);

        try
        {
            var handlerTypes = GetHandlerTypes(eventType);
            
            if (handlerTypes.Length == 0)
            {
                _logger.LogWarning("No handlers found for domain event: {EventType}", eventType.Name);
                return;
            }

            var tasks = handlerTypes.Select(handlerType => HandleEventAsync(@event, handlerType));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully published domain event: {EventType} to {HandlerCount} handlers", 
                eventType.Name, handlerTypes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event: {EventType}", eventType.Name);
            throw;
        }
    }

    private async Task HandleEventAsync<TEvent>(TEvent @event, Type handlerType) where TEvent : IDomainEvent
    {
        try
        {
            // Get the handler interface type (IDomainEventHandler<T>)
            var eventType = typeof(TEvent);
            var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            
            var handler = _serviceProvider.GetService(handlerInterface);
            if (handler == null)
            {
                _logger.LogWarning("Handler {HandlerType} not found in DI container for event {EventType}", 
                    handlerType.Name, eventType.Name);
                return;
            }

            var handleMethod = handlerInterface.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                _logger.LogError("HandleAsync method not found in handler interface {HandlerInterface}", 
                    handlerInterface.Name);
                return;
            }

            var task = (Task)handleMethod.Invoke(handler, [@event])!;
            await task;

            _logger.LogDebug("Successfully handled domain event {EventType} with handler {HandlerType}", 
                eventType.Name, handlerType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}", 
                typeof(TEvent).Name, handlerType.Name);
            throw;
        }
    }

    private static Type[] GetHandlerTypes(Type eventType)
    {
        return HandlerTypesCache.GetOrAdd(eventType, type =>
        {
            var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(type);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetInterfaces().Any(i => i == handlerInterface))
                .ToArray();
        });
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        var eventList = domainEvents.ToList();
        _logger.LogInformation("Publishing {EventCount} domain events at {Timestamp}", 
            eventList.Count, DateTime.UtcNow);

        try
        {
            var tasks = eventList.Select(@event => PublishEventByType(@event));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully published {EventCount} domain events", eventList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing {EventCount} domain events", eventList.Count);
            throw;
        }
    }
    
    private async Task PublishEventByType(IDomainEvent @event)
    {
        var eventType = @event.GetType();
        _logger.LogInformation("Publishing domain event: {EventType} at {Timestamp}", 
            eventType.Name, DateTime.UtcNow);

        try
        {
            var handlerTypes = GetHandlerTypes(eventType);
            
            if (handlerTypes.Length == 0)
            {
                _logger.LogWarning("No handlers found for domain event: {EventType}", eventType.Name);
                return;
            }

            var tasks = handlerTypes.Select(handlerType => HandleEventByType(@event, handlerType));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully published domain event: {EventType} to {HandlerCount} handlers", 
                eventType.Name, handlerTypes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event: {EventType}", eventType.Name);
            throw;
        }
    }
    
    private async Task HandleEventByType(IDomainEvent @event, Type handlerType)
    {
        try
        {
            // Get the handler interface type (IDomainEventHandler<T>)
            var eventType = @event.GetType();
            var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            
            var handler = _serviceProvider.GetService(handlerInterface);
            if (handler == null)
            {
                _logger.LogWarning("Handler {HandlerType} not found in DI container for event {EventType}", 
                    handlerType.Name, eventType.Name);
                return;
            }

            var handleMethod = handlerInterface.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                _logger.LogError("HandleAsync method not found in handler interface {HandlerInterface}", 
                    handlerInterface.Name);
                return;
            }

            var task = (Task)handleMethod.Invoke(handler, [@event])!;
            await task;

            _logger.LogDebug("Successfully handled domain event {EventType} with handler {HandlerType}", 
                eventType.Name, handlerType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}", 
                @event.GetType().Name, handlerType.Name);
            throw;
        }
    }
}
