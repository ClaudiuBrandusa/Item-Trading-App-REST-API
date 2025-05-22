using Application.Utils.Notifications;
using Infrastructure.Services.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Infrastructure.Installers;

public class HubClientsWrapperInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IHubClientsWrapper, HubClientsWrapper>();
    }
}
