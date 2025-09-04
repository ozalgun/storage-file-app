using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Infrastructure.Data;
using StorageFileApp.Domain.Events;
using Microsoft.Extensions.Logging;

namespace StorageFileApp.Infrastructure.Repositories;

public class UnitOfWork(IDbContextFactory<StorageFileDbContext> contextFactory, IDomainEventPublisher domainEventPublisher, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly IDbContextFactory<StorageFileDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    private readonly IDomainEventPublisher _domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
    private readonly ILogger<UnitOfWork> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private StorageFileDbContext? _context;
    private IDbContextTransaction? _transaction;

    public async Task<int> SaveChangesAsync()
    {
        if (_context == null)
        {
            _context = await _contextFactory.CreateDbContextAsync();
        }

        // 1. Collect domain events from all tracked entities
        var domainEvents = CollectDomainEvents();
        
        // 2. Save changes to database
        var result = await _context.SaveChangesAsync();
        
        // 3. Publish domain events after successful save
        if (domainEvents.Any())
        {
            _logger.LogInformation("Publishing {EventCount} domain events after SaveChangesAsync", domainEvents.Count);
            await _domainEventPublisher.PublishAsync(domainEvents);
        }
        
        return result;
    }
    
    
    private List<IDomainEvent> CollectDomainEvents()
    {
        if (_context == null)
        {
            return new List<IDomainEvent>();
        }
        return CollectDomainEvents(_context);
    }
    
    private List<IDomainEvent> CollectDomainEvents(StorageFileDbContext context)
    {
        var domainEvents = new List<IDomainEvent>();
        
        // Get all tracked entities that might have domain events
        var trackedEntities = context.ChangeTracker.Entries()
            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added ||
                       e.State == Microsoft.EntityFrameworkCore.EntityState.Modified ||
                       e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted)
            .Select(e => e.Entity)
            .ToList();
        
        // Collect domain events from entities that implement domain event collection
        foreach (var entity in trackedEntities)
        {
            if (entity is IHasDomainEvents hasDomainEvents)
            {
                domainEvents.AddRange(hasDomainEvents.DomainEvents);
                hasDomainEvents.ClearDomainEvents();
            }
        }
        
        return domainEvents;
    }

    public async Task BeginTransactionAsync()
    {
        if (_context == null)
        {
            _context = await _contextFactory.CreateDbContextAsync();
        }
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}
