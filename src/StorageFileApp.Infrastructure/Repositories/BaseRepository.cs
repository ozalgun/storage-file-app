using Microsoft.EntityFrameworkCore;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Infrastructure.Data;
using StorageFileApp.Domain.Events;
using System.Linq.Expressions;

namespace StorageFileApp.Infrastructure.Repositories;

public class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IDbContextFactory<StorageFileDbContext> _contextFactory;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IDomainEventPublisher _domainEventPublisher;

    protected BaseRepository(IDbContextFactory<StorageFileDbContext> contextFactory, IUnitOfWork unitOfWork, IDomainEventPublisher domainEventPublisher)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
    }

    protected async Task<StorageFileDbContext> GetContextAsync()
    {
        return await _contextFactory.CreateDbContextAsync();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        using var context = await GetContextAsync();
        return await context.Set<T>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        using var context = await GetContextAsync();
        return await context.Set<T>().ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        using var context = await GetContextAsync();
        return await context.Set<T>().Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        using var context = await GetContextAsync();
        await context.Set<T>().AddAsync(entity);
        
        // Collect and publish domain events
        var domainEvents = CollectDomainEvents(context);
        await context.SaveChangesAsync();
        
        if (domainEvents.Any())
        {
            await _domainEventPublisher.PublishAsync(domainEvents);
        }
        
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        using var context = await GetContextAsync();
        context.Set<T>().Update(entity);
        
        // Collect and publish domain events
        var domainEvents = CollectDomainEvents(context);
        await context.SaveChangesAsync();
        
        if (domainEvents.Any())
        {
            await _domainEventPublisher.PublishAsync(domainEvents);
        }
    }

    public virtual async Task DeleteAsync(T entity)
    {
        using var context = await GetContextAsync();
        context.Set<T>().Remove(entity);
        
        // Collect and publish domain events
        var domainEvents = CollectDomainEvents(context);
        await context.SaveChangesAsync();
        
        if (domainEvents.Any())
        {
            await _domainEventPublisher.PublishAsync(domainEvents);
        }
    }

    public virtual async Task DeleteByIdAsync(Guid id)
    {
        using var context = await GetContextAsync();
        var entity = await context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            context.Set<T>().Remove(entity);
            
            // Collect and publish domain events
            var domainEvents = CollectDomainEvents(context);
            await context.SaveChangesAsync();
            
            if (domainEvents.Any())
            {
                await _domainEventPublisher.PublishAsync(domainEvents);
            }
        }
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        using var context = await GetContextAsync();
        return await context.Set<T>().FindAsync(id) != null;
    }

    public virtual async Task<int> CountAsync()
    {
        using var context = await GetContextAsync();
        return await context.Set<T>().CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        using var context = await GetContextAsync();
        return await context.Set<T>().CountAsync(predicate);
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
}
