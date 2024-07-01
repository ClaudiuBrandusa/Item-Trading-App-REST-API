using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Infrastructure.Installers;

public class DatabaseContextWrapperInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDatabaseContextWrapper, DatabaseContextWrapper>();
    }
}
