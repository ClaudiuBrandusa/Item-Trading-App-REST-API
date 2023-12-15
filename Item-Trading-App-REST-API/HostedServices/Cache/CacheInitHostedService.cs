using Item_Trading_App_REST_API.Options;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using MediatR;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.HostedServices.Cache;

public class CacheInitHostedService : IHostedService
{
    private readonly IMediator _mediator;
    private readonly CacheSettings _settings;

    public CacheInitHostedService(IMediator mediator, CacheSettings settings)
    {
        _mediator = mediator;
        _settings = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.InitAtStartup) return;

        // using listItems from the item service will set the cache if it is a miss
        var itemsId = (await _mediator.Send(new ListItemsQuery(), cancellationToken)).ItemsId.ToArray();

        // init used items
        for (int i = 0; i < itemsId.Length; i++)
        {
            await _mediator.Send(new ItemUsedInTradeQuery { ItemId = itemsId[i] }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
