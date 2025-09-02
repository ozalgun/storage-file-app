using Microsoft.EntityFrameworkCore.Storage;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Infrastructure.Data;

namespace StorageFileApp.Infrastructure.Repositories;

public class UnitOfWork(StorageFileDbContext context) : IUnitOfWork
{
    private readonly StorageFileDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private IDbContextTransaction? _transaction;

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
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
        _context.Dispose();
    }
}
