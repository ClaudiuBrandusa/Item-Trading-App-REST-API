using Item_Trading_App_REST_API.HostedServices.Identity.RefreshToken;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Item_Trading_App_REST_API.Installers;

public class HostedServicesInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<RefreshTokenHostedService>();
    }
}
