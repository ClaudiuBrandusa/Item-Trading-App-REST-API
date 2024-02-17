using Item_Trading_App_REST_API.Data;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.DatabaseContextWrapper;

public interface IDatabaseContextWrapper
{
    DatabaseContext ProvideDatabaseContext();

    Task<DatabaseContext> ProvideDatabaseContextAsync();

    void Dispose(DatabaseContext context);
}
