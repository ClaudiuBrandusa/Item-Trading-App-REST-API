using Item_Trading_App_REST_API.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.DatabaseContextWrapper;

public class DatabaseContextWrapper : IDatabaseContextWrapper
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public DatabaseContextWrapper(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public DatabaseContext ProvideDatabaseContext()
    {
        return _dbContextFactory.CreateDbContext();
    }

    public Task<DatabaseContext> ProvideDatabaseContextAsync()
    {
        return _dbContextFactory.CreateDbContextAsync();
    }

    public void Dispose(DatabaseContext context)
    {
        context?.Dispose();
    }
}
