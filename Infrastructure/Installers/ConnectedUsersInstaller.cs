using Application.Services.ConnectedUsers;
using Infrastructure.Services.ConnectedUsers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Infrastructure.Installers;

public class ConnectedUsersInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectedUsersRepository, ConnectedUsersRepository>();
    }
}
