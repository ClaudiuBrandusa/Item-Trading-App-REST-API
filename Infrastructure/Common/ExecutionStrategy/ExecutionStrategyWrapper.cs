using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.ExecutionStrategy;
public class ExecutionStrategyWrapper : IExecutionStrategyWrapper
{
    private readonly IExecutionStrategy _executionStrategy;

    public ExecutionStrategyWrapper(IExecutionStrategy executionStrategy)
    {
        _executionStrategy = executionStrategy;
    }

    public Task ExecuteAsync(Func<Task> func)
    {
        return _executionStrategy.ExecuteAsync(func);
    }
}
