using Application.Services.UnitOfWork;
using Infrastructure.Services.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Infrastructure.Installers;

public class UnitOfWorkInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWorkService, UnitOfWorkService>();
    }
}
