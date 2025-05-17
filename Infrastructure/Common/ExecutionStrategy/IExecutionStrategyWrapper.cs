namespace Infrastructure.Common.ExecutionStrategy;

public interface IExecutionStrategyWrapper
{
    Task ExecuteAsync(Func<Task> func);
}
