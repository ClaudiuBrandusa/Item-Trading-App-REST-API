using Microsoft.EntityFrameworkCore.Storage;

namespace Item_Trading_App_REST_API.Services.UnitOfWork;

public interface IUnitOfWorkService
{
    IDbContextTransaction Transaction { get; }

    void BeginTransaction();

    void CommitTransaction();

    void RollbackTransaction();
}
