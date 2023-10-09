using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public class MvcInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
    }
}
