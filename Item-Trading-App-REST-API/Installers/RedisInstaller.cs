using Item_Trading_App_REST_API.Options;
using Item_Trading_App_REST_API.Services.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Item_Trading_App_REST_API.Installers
{
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
}
