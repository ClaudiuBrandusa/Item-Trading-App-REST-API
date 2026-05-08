using Application.Behaviors.Item.ListItems;
using Application.Behaviors.Trade.ItemUsedInTrade;
using Application.Options;
using Item_Trading_App_REST_API.StartupServices.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.StartupServices;

public class CacheInitStartupService : IStartupService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CacheSettings _settings;

    public CacheInitStartupService(IServiceProvider serviceProvider, CacheSettings settings)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
    }

    public async Task Execute()
    {
        if (!_settings.InitAtStartup) return;

        var cancellationToken = CancellationToken.None;

        using var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var sender = scope.ServiceProvider.GetService<ISender>();

        // using listItems from the item service will set the cache if it is a miss
        var itemsResult = await sender.Send(new ListItemsQuery(), cancellationToken);
        var itemsId = itemsResult.Content.ItemsId.ToArray();

        // init used items
        for (int i = 0; i < itemsId.Length; i++)
        {
            await sender.Send(new ItemUsedInTradeQuery { ItemId = itemsId[i] }, cancellationToken);
        }
    }
}
