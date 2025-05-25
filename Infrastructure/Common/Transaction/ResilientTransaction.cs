using Infrastructure.Common.DatabaseContextTransaction;

namespace Infrastructure.Common.Transaction;

public class ResilientTransaction
{
    private IDatabaseContextTransactionWrapper _context;

    private bool commitTransaction;

    private ResilientTransaction(IDatabaseContextTransactionWrapper context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    public static ResilientTransaction New(IDatabaseContextTransactionWrapper context) =>
        new ResilientTransaction(context);

    public async Task<T?> ExecuteAsync<T>(Func<TaskCompletionSource<T?>, Task<bool>> func) where T : class
    {
        var taskCompletionSource = new TaskCompletionSource<T?>();
        var task = taskCompletionSource.Task;

        T? result = null;
        commitTransaction = false;

        var strategy = _context.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.BeginTransactionAsync();

            try
            {
                commitTransaction = await func(taskCompletionSource);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception catched in {nameof(ResilientTransaction)}: {ex.Message}");
            }

            if (task.IsCompletedSuccessfully)
            {
                result = await task;
            }
            else if (task.IsCompleted || task.IsCanceled)
            {
                commitTransaction = false;
            }
            else
            {
                // make sure that the task finished
                taskCompletionSource.SetResult(null);
            }

            if (commitTransaction)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
        });

        return result;
    }

    public async Task ExecuteAsync(Func<Task<bool>> func)
    {
        commitTransaction = false;

        var strategy = _context.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.BeginTransactionAsync();

            try
            {
                commitTransaction = await func();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception catched in {nameof(ResilientTransaction)}: {ex.Message}");
            }

            if (commitTransaction)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
        });
    }
}
