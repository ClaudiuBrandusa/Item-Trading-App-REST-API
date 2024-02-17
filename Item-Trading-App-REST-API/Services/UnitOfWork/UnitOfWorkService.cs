using System;
using System.Transactions;

namespace Item_Trading_App_REST_API.Services.UnitOfWork;

public class UnitOfWorkService : IUnitOfWorkService, IDisposable
{
    private TransactionScope _transaction;

    public void BeginTransaction()
    {
        if (OperatingSystem.IsWindows())
            TransactionManager.ImplicitDistributedTransactions = true;
        _transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        TransactionInterop.GetTransmitterPropagationToken(Transaction.Current);
    }

    public void CommitTransaction()
    {
        if (_transaction is not null)
            ClearTransaction();
    }

    public void RollbackTransaction()
    {
        if (_transaction is not null)
            ClearTransaction();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ClearTransaction()
    {
        _transaction.Complete();
        _transaction.Dispose();
        _transaction = null;
    }
}
