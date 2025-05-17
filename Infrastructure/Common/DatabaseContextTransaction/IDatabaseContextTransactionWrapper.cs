using Infrastructure.Common.ExecutionStrategy;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Common.DatabaseContextTransaction;

public interface IDatabaseContextTransactionWrapper
{
    IExecutionStrategyWrapper CreateExecutionStrategy();

    Task<IDbContextTransaction> BeginTransactionAsync();
}
