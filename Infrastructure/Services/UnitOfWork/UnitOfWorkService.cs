using Application.Services.UnitOfWork;
using Infrastructure.Common.DatabaseContextTransaction;
using Infrastructure.Common.Transaction;
using Infrastructure.Services.DatabaseContextWrapper;

namespace Infrastructure.Services.UnitOfWork;

public class UnitOfWorkService : IUnitOfWorkService
{
    private readonly IDatabaseContextWrapper _databaseContextWrapper;

    public UnitOfWorkService(IDatabaseContextWrapper databaseContextWrapper)
    {
        _databaseContextWrapper = databaseContextWrapper;
    }

    public async Task ExplicitTransaction(Func<Task<bool>> action)
    {
        var dbContext = _databaseContextWrapper.ProvideDatabaseContext();

        var databaseContextTransactionWrapper = new DatabaseContextTransactionWrapper(dbContext);

        var resilientTransaction = ResilientTransaction.New(databaseContextTransactionWrapper);

        await resilientTransaction.ExecuteAsync(action);

        _databaseContextWrapper.DisposeDatabaseContext(dbContext);
    }

    public async Task<T?> ExplicitTransaction<T>(Func<TaskCompletionSource<T?>, Task<bool>> action) where T : class
    {
        var dbContext = _databaseContextWrapper.ProvideDatabaseContext();

        var databaseContextTransactionWrapper = new DatabaseContextTransactionWrapper(dbContext);

        var resilientTransaction = ResilientTransaction.New(databaseContextTransactionWrapper);

        var result = await resilientTransaction.ExecuteAsync(action);

        _databaseContextWrapper.DisposeDatabaseContext(dbContext);

        return result;
    }
}
