using Application.Services.RefreshToken;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Application.Installers;

public class RefreshTokenInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    }
}
