using Application.Services.TradeItem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Application.Installers;

public class TradeItemInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITradeItemService, TradeItemService>();
    }
}
