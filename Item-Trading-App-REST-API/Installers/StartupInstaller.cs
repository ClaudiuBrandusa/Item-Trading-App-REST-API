using Item_Trading_App_REST_API.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public class StartupInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IStartupFilter, StartupFilter>();
    }
}
