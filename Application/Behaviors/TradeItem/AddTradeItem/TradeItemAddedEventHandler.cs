using Application.Constants;
using Application.Services.Cache;
using MediatR;

namespace Application.Behaviors.TradeItem.AddTradeItem;

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
