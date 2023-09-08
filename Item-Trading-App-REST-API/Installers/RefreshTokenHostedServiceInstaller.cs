using Item_Trading_App_REST_API.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers
{
    public class RefreshTokenHostedServiceInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
            var settings = new RefreshTokenSettings();
            configuration.Bind(nameof(RefreshTokenSettings), settings);
            services.AddSingleton(settings);
        }
    }
}
