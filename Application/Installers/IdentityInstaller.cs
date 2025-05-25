using Application.Services.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Application.Installers;

public class IdentityInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIdentityService, IdentityService>();
    }
}
