using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public interface IInstaller
{
    void InstallServices(IServiceCollection services, IConfiguration configuration);
}
