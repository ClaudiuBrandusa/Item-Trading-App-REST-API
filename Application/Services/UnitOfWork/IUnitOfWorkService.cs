namespace Application.Services.UnitOfWork;

public interface IUnitOfWorkService
{
    void BeginTransaction();

    void CommitTransaction();

    void RollbackTransaction();
}
