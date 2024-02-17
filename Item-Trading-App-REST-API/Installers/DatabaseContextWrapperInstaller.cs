using Item_Trading_App_REST_API.Services.DatabaseContextWrapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public class DatabaseContextWrapperInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDatabaseContextWrapper, DatabaseContextWrapper>();
    }
}
