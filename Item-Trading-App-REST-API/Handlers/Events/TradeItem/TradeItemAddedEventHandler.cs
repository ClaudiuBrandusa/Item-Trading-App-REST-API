using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Resources.Events.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Events.TradeItem;

public class TradeItemAddedEventHandler : INotificationHandler<TradeItemAddedEvent>
{
    private readonly ICacheService _cacheService;

    public TradeItemAddedEventHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task Handle(TradeItemAddedEvent notification, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.TradeItem.GetTradeItemKey(notification.TradeId, notification.Data.ItemId), notification.Data),
            _cacheService.AddToSet(CacheKeys.UsedItem.GetUsedItemKey(notification.Data.ItemId), notification.TradeId)
        );
    }
}
