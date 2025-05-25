using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Infrastructure.IntegrationTests.Common;

public class CachingFixture : IAsyncLifetime
{
    private readonly RedisContainer _cachingContainer;

    public IServiceProvider ServiceProvider;

    public IConfiguration Configuration;

    public CachingFixture()
    {
        _cachingContainer = new RedisBuilder()
            .WithImage("redis:latest")
            .WithCleanUp(true)
            .WithPortBinding(6379, assignRandomHostPort: true)
            .Build();
    }

    protected virtual void RegisterServices(IServiceCollection services)
    {
        Application.DependencyInjection.AddApplication(services, Configuration);
        DependencyInjection.AddInfrastructure(services, Configuration);
    }

    public async Task InitializeAsync()
    {
        await _cachingContainer.StartAsync();

        var configurationValues = new Dictionary<string, string>
        {
            { "RedisSettings:ConnectionAddress", _cachingContainer.GetConnectionString() }
        };

        Configuration = ConfigurationMock.BuildConfiguration(configurationValues);

        var services = new ServiceCollection();

        RegisterServices(services);

        ServiceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _cachingContainer.DisposeAsync();
    }

    public static async Task<CachingFixture> BuildDatabaseFixture()
    {
        var cachingFixture = new CachingFixture();

        await cachingFixture.InitializeAsync();

        return cachingFixture;
    }
}
