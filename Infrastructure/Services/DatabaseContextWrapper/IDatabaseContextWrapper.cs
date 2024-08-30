using Infrastructure.Data;

namespace Infrastructure.Services.DatabaseContextWrapper;

public interface IDatabaseContextWrapper
{
    DatabaseContext ProvideDatabaseContext();

    Task<DatabaseContext> ProvideDatabaseContextAsync();

    void DisposeDatabaseContext(DatabaseContext context);
}
