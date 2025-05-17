using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.DatabaseContextWrapper;

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

    public void DisposeDatabaseContext(DatabaseContext context)
    {
        context?.Dispose();
    }
}
