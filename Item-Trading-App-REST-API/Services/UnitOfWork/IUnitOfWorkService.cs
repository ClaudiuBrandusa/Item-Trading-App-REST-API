namespace Item_Trading_App_REST_API.Services.UnitOfWork;

public interface IUnitOfWorkService
{
    void BeginTransaction();

    void CommitTransaction();

    void RollbackTransaction();
}
