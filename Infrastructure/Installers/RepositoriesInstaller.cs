using Application.Repositories;
using Domain.Repositories;
using Domain.Repositories.Identity;
using Domain.Repositories.Inventories;
using Domain.Repositories.Items;
using Domain.Repositories.Trades;
using Infrastructure.Repositories.Identity;
using Infrastructure.Repositories.Inventories;
using Infrastructure.Repositories.Items;
using Infrastructure.Repositories.Trades;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Infrastructure.Installers;

public class RepositoriesInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        // cached repositories
        services.AddScoped<ICachedItemRepository, CachedItemRepository>();
        services.AddScoped<ICachedInventoryRepository, CachedInventoryRepository>();
        services.AddScoped<ICachedTradeRepository, CachedTradeRepository>();

        // repositories
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IUserRepository, IdentityRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
    }
}
