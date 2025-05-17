namespace Application.Services.UnitOfWork;

public interface IUnitOfWorkService
{
    Task ExplicitTransaction(Func<Task<bool>> action);

    Task<T?> ExplicitTransaction<T>(Func<TaskCompletionSource<T?>, Task<bool>> action) where T : class;
}
