using Infrastructure.Common.ExecutionStrategy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Common.DatabaseContextTransaction;

public class DatabaseContextTransactionWrapper : IDatabaseContextTransactionWrapper
{
    private readonly DatabaseContext _databaseContext;

    public DatabaseContextTransactionWrapper(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public IExecutionStrategyWrapper CreateExecutionStrategy()
    {
        var executionStrategyWrapper = new ExecutionStrategyWrapper(_databaseContext.Database.CreateExecutionStrategy());

        return executionStrategyWrapper;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _databaseContext.Database.BeginTransactionAsync();
    }
}
