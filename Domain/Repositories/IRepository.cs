namespace Domain.Repositories;

public interface IRepository
{
    /// <summary>
    /// Adds the entity and saves after that.
    /// </summary>
    /// <returns>Operation status</returns>
    Task<bool> AddEntityAsync<T>(T entity) where T : class;

    /// <summary>
    /// Updates the entity and saves after that.
    /// </summary>
    /// <returns>Operation status</returns>
    Task<bool> UpdateEntityAsync<T>(T entity) where T : class;

    /// <summary>
    /// Will add or update depending on the given condition. True will execute the add method and false will execute the update method.
    /// </summary>
    /// <returns>Operation status</returns>
    Task<bool> AddOrUpdateEntityAsync<T>(T entity, Func<bool> condition) where T : class;

    /// <summary>
    /// Removes the entity and saves after that.
    /// </summary>
    /// <returns>Operation status</returns>
    Task<bool> RemoveEntityAsync<T>(T entity) where T : class;

    /// <summary>
    /// Detaches a tracked entity from the current context
    /// </summary>
    void DetachEntity<T>(T entity) where T : class;

    /// <summary>
    /// Starts a new transaction executing on the current database context instance.
    /// </summary>
    /// <returns></returns>
    Task<bool> StartTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Commit the current transaction.
    /// </summary>
    /// <returns></returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Rollback the current transaction.
    /// </summary>
    /// <returns></returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the changes
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync();
}
