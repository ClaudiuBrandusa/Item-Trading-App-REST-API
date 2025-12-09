using Infrastructure.Wrappers.Hubs;
using Item_Trading_App_REST_API.Hubs;
using Item_Trading_App_REST_API.Wrappers.Hubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Item_Trading_App_REST_API.Installers;

public class SignalRInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();
        services.AddSingleton<NotificationHubBase, NotificationHub>();
        services.AddTransient<IHubContextWrapper, HubContextWrapper>();
    }
}
