using Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Infrastructure.Installers;

public class CacheSettingsInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        var cacheSettings = new CacheSettings();
        configuration.Bind(nameof(CacheSettings), cacheSettings);
        services.AddSingleton(cacheSettings);
    }
}
