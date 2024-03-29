﻿using Item_Trading_App_REST_API.Services.Trade;
using Item_Trading_App_REST_API.Services.TradeItem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Item_Trading_App_REST_API.Installers;

public class TradeInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITradeService, TradeService>();
        services.AddScoped<ITradeItemService, TradeItemService>();
    }
}
