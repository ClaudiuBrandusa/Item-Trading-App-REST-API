using Domain.Repositories;
using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Repositories;

public abstract class RepositoryBase : IRepository, IDisposable
{
    protected IDatabaseContextWrapper DatabaseContextWrapper { get; init; }
    protected readonly DatabaseContext context;

    public RepositoryBase(IDatabaseContextWrapper databaseContextWrapper)
    {
        DatabaseContextWrapper = databaseContextWrapper;
        context = databaseContextWrapper.ProvideDatabaseContext();
    }

    public async Task<bool> AddEntityAsync<T>(T entity) where T : class
    {
        var context = DatabaseContextWrapper.ProvideDatabaseContext();
        try
        {
            if (!IsEntityValid(entity)) return false;
            await context.AddAsync(entity);
            var added = await context.SaveChangesAsync();
            context.Entry(entity).State = EntityState.Detached;
            return added > 0;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception ex)
        {
            
        }
        finally
        {
            DatabaseContextWrapper.DisposeDatabaseContext(context);
        }

        return false;
    }

    public async Task<bool> UpdateEntityAsync<T>(T entity) where T : class
    {
        try
        {
            if (!IsEntityValid(entity)) return false;
            context.Update(entity);
            var updated = await context.SaveChangesAsync();
            context.Entry(entity).State = EntityState.Detached;
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
            context.Remove(entity);
            var removed = await context.SaveChangesAsync();
            context.Entry(entity).State = EntityState.Detached;
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

    public Task<int> SaveChangesAsync()
    {
        return context.SaveChangesAsync();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DatabaseContextWrapper.DisposeDatabaseContext(context);
    }

    private bool IsEntityValid<T>(T entity)
    {
        if (entity is null) return false;

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(entity, null, null);

        bool isValid = Validator.TryValidateObject(entity, validationContext, validationResults, true);

        return isValid;
    }
}
