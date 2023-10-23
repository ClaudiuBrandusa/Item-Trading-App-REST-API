using Item_Trading_App_REST_API.Services.Inventory;
using Item_Trading_App_REST_API.Services.Item;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public class ItemInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IInventoryService, InventoryService>();
    }
}
