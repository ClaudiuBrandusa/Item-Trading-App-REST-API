using Item_Trading_App_REST_API.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace Item_Trading_App_REST_API.Services.UnitOfWork;

public class UnitOfWorkService : IUnitOfWorkService, IDisposable
{
    private readonly DatabaseContext _context;
    private IDbContextTransaction _transaction;

    public IDbContextTransaction Transaction
    {
        get
        {
            return _transaction;
        }
    }

    public UnitOfWorkService(DatabaseContext context)
    {
        _context = context;
    }

    public void BeginTransaction()
    {
        _transaction = _context.Database.BeginTransaction();
    }

    public void CommitTransaction()
    {
        _transaction?.Commit();
    }

    public void RollbackTransaction()
    {
        _transaction?.Rollback();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}
