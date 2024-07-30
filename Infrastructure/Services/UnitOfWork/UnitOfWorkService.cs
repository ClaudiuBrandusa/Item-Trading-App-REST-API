using Application.Services.UnitOfWork;
using System.Transactions;

namespace Infrastructure.Services.UnitOfWork;

public class UnitOfWorkService : IUnitOfWorkService, IDisposable
{
    private TransactionScope? _transaction;

    public void BeginTransaction()
    {
        _transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
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
        if (_transaction is null) return;

        _transaction.Complete();
        _transaction.Dispose();
        _transaction = null;
    }
}
