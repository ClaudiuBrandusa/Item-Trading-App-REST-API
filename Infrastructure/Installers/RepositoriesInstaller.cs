using Application.Repositories;
using Domain.Repositories;
using Domain.Repositories.Inventory;
using Domain.Repositories.Items;
using Domain.Repositories.TradeItems;
using Domain.Repositories.TradeItemsHistory;
using Domain.Repositories.Trades;
using Infrastructure.Repositories.Identity;
using Infrastructure.Repositories.Inventory;
using Infrastructure.Repositories.Items;
using Infrastructure.Repositories.TradeItems;
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
        services.AddScoped<ICachedTradeItemRepository, CachedTradeItemRepository>();
        services.AddScoped<ICachedTradeItemHistoryRepository, CachedTradeItemHistoryRepository>();

        // repositories
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IUserRepository, IdentityRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<ITradeItemRepository, TradeContentRepository>();
        services.AddScoped<ITradeItemHistoryRepository, TradeItemHistoryRepository>();
    }
}
