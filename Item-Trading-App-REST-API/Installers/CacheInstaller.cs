using Item_Trading_App_REST_API.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public class CacheInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        var cacheSettings = new CacheSettings();
        configuration.Bind(nameof(CacheSettings), cacheSettings);
        services.AddSingleton(cacheSettings);
    }
}
