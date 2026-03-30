using Application.Installers;
using Application.Options;
using Application.Services.Cache;
using Application.Services.Notification;
using Application.Utils;
using Infrastructure.Installers;
using Infrastructure.IntegrationTests.Common.Mocks;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.MsSql;

namespace Infrastructure.IntegrationTests.Common.Fixtures;
public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;

    public IServiceProvider? ServiceProvider;

    public IConfiguration? Configuration;

    public DatabaseFixture()
    {
        _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:latest")
            .WithPassword("YourStrong!Passw0rd")
            .Build();
    }

    protected virtual void RegisterServices(IServiceCollection services)
    {
        var dbInstaller = new DbInstaller();
        dbInstaller.InstallServices(services, Configuration!);
        var unitOfWorkInstaller = new UnitOfWorkInstaller();
        unitOfWorkInstaller.InstallServices(services, Configuration!);
        var mediatorInstaller = new MediatorInstaller();
        mediatorInstaller.InstallServices(services, Configuration!);
        var dbContextWrapperInstaller = new DatabaseContextWrapperInstaller();
        dbContextWrapperInstaller.InstallServices(services, Configuration!);
        var mapsterInstaller = new MapsterInstaller();
        mapsterInstaller.InstallServices(services, Configuration!);
        var jwtSettings = new JwtSettings
        {
            AllowedRefreshTokensPerUser = 3,
            RefreshTokenLifetime = TimeSpan.FromHours(1),
            TokenLifetime = TimeSpan.FromHours(3),
            Secret = "1234"
        };
        services.AddSingleton(jwtSettings);
        var tokenValidationParameters = JwtUtils.BuildTokenValidationParameters(jwtSettings.Secret);
        services.AddSingleton(tokenValidationParameters);
        var identityInstaller = new IdentityInstaller();
        identityInstaller.InstallServices(services, Configuration!);
        var refreshTokenInstaller = new RefreshTokenInstaller();
        refreshTokenInstaller.InstallServices(services, Configuration!);
        var itemInstaller = new ItemInstaller();
        itemInstaller.InstallServices(services, Configuration!);
        var inventoryInstaller = new InventoryInstaller();
        inventoryInstaller.InstallServices(services, Configuration!);
        var tradeServiceInstaller = new TradeInstaller();
        tradeServiceInstaller.InstallServices(services, Configuration!);
        services.AddScoped((_) => new Mock<IClientNotificationService>().Object);
        var repositoriesInstaller = new RepositoriesInstaller();
        repositoriesInstaller.InstallServices(services, Configuration!);
        var cacheServiceMock = new Mock<ICacheService>();
        var cacheServiceImpl = cacheServiceMock.Object;
        services.AddScoped((_) => cacheServiceImpl);
    }

    public async ValueTask InitializeAsync()
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

    public async ValueTask DisposeAsync()
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
