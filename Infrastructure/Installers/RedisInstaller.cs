using Application.Services.Cache;
using Infrastructure.Options;
using Infrastructure.Services.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;
using StackExchange.Redis;

namespace Infrastructure.Installers;

public class RedisInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = new RedisSettings();
        configuration.Bind(nameof(RedisSettings), redisSettings);

        services.AddSingleton<IConnectionMultiplexer>(x =>
            ConnectionMultiplexer.Connect(redisSettings.ConnectionAddress));

        services.AddSingleton<ICacheService, RedisCacheService>();
    }
}
