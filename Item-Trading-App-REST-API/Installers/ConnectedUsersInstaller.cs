using Item_Trading_App_REST_API.Services.ConnectedUsers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public class ConnectedUsersInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectedUsersRepository, ConnectedUsersRepository>();
    }
}
