using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Repositories;

public abstract class RepositoryBase : IRepository
{
    protected readonly DatabaseContext _context;

    public RepositoryBase(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<bool> AddEntityAsync<T>(T entity) where T : class
    {
        try
        {
            if (!IsEntityValid(entity)) return false;
            await _context.AddAsync(entity);
            var added = await _context.SaveChangesAsync();
            _context.Entry(entity).State = EntityState.Detached;
            return added > 0;
        }
        catch (InvalidOperationException)
        {
        }

        return false;
    }

    public async Task<bool> UpdateEntityAsync<T>(T entity) where T : class
    {
        try
        {
            if (!IsEntityValid(entity)) return false;
            _context.Update(entity);
            var updated = await _context.SaveChangesAsync();
            _context.Entry(entity).State = EntityState.Detached;
            return updated > 0;
        }
        catch (DbUpdateException)
        {
        }

        return false;
    }

    public async Task<bool> RemoveEntityAsync<T>(T entity) where T : class
    {
        try
        {
            _context.Remove(entity);
            var removed = await _context.SaveChangesAsync();
            _context.Entry(entity).State = EntityState.Detached;
            return removed > 0;
        }
        catch (DbUpdateConcurrencyException)
        {
        }

        return false;
    }

    public Task<bool> AddOrUpdateEntityAsync<T>(T entity, Func<bool> condition) where T : class
    {
        if (condition())
        {
            return AddEntityAsync(entity);
        }
        else
        {
            return UpdateEntityAsync(entity);
        }
    }

    public void DetachEntity<T>(T entity) where T : class
    {
        _context.Entry(entity).State = EntityState.Detached;
    }

    private bool IsEntityValid<T>(T entity)
    {
        if (entity is null) return false;

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(entity, null, null);

        bool isValid = Validator.TryValidateObject(entity, validationContext, validationResults, true);

        return isValid;
    }

    /// <returns>True means that new transaction was created;
    /// False means that a transaction was already active so the current transaction 
    /// was joined as an inner transaction</returns>
    public async Task<bool> StartTransactionAsync(CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            await _context.Database.BeginTransactionAsync(cancellationToken);
            return true;
        }

        return false;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        await _context.Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        await _context.Database.RollbackTransactionAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
