using Application.Installers;
using Infrastructure.Installers;
using Infrastructure.IntegrationTests.Common.Mocks;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Infrastructure.IntegrationTests.Common.Fixtures;
public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;

    public IServiceProvider ServiceProvider;

    public IConfiguration Configuration;

    public DatabaseFixture()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:latest")
            .WithPassword("YourStrong!Passw0rd")
            .Build();
    }

    protected virtual void RegisterServices(IServiceCollection services)
    {
        var dbInstaller = new DbInstaller();
        dbInstaller.InstallServices(services, Configuration);
        var unitOfWorkInstaller = new UnitOfWorkInstaller();
        unitOfWorkInstaller.InstallServices(services, Configuration);
        var mediatorInstaller = new MediatorInstaller();
        mediatorInstaller.InstallServices(services, Configuration);
        var dbContextWrapperInstaller = new DatabaseContextWrapperInstaller();
        dbContextWrapperInstaller.InstallServices(services, Configuration);
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var configurationValues = new Dictionary<string, string>
        {
            { "ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString() }
        };

        Configuration = ConfigurationMock.BuildConfiguration(configurationValues);

        var services = new ServiceCollection();

        RegisterServices(services);

        ServiceProvider = services.BuildServiceProvider();

        var dbContextWrapper = ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        await using var context = dbContextWrapper.ProvideDatabaseContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public static async Task<DatabaseFixture> BuildDatabaseFixture()
    {
        var dbFixture = new DatabaseFixture();

        await dbFixture.InitializeAsync();

        return dbFixture;
    }
}
