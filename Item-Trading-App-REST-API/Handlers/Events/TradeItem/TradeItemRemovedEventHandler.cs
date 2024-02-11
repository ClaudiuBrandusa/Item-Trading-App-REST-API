using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Resources.Events.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.TradeItem;

public class TradeItemRemovedEventHandler : INotificationHandler<TradeItemRemovedEvent>
{
    private readonly ICacheService _cacheService;

    public TradeItemRemovedEventHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task Handle(TradeItemRemovedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.KeepCache)
        {
            return _cacheService.ClearCacheKeysStartingWith(CacheKeys.TradeItem.GetTradeItemKey(notification.TradeId, ""));
        }

        return Task.CompletedTask;
    }
}
