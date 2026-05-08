using Item_Trading_App_REST_API.StartupServices;
using Item_Trading_App_REST_API.StartupServices.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

public class StartupServicesInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IStartupService, CacheInitStartupService>();
    }
}
