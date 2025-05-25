using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;
using System.Reflection;

namespace Application.Installers;

public class MediatorInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(MediatorInstaller))!);
        });
    }
}
