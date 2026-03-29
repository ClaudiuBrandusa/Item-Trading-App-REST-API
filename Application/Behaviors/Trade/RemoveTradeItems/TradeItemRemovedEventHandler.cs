using Application.Constants;
using Application.Services.Cache;
using Domain.DomainEvents.Trades;
using MediatR;

namespace Application.Behaviors.Trade.RemoveTradeItems;

public class TradeItemRemovedEventHandler : INotificationHandler<TradeItemRemovedDomainEvent>
{
    private readonly ICacheService _cacheService;

    public TradeItemRemovedEventHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task Handle(TradeItemRemovedDomainEvent notification, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            _cacheService.ClearCacheKeyAsync(CacheKeys.TradeItem.GetTradeItemKey(notification.TradeId, notification.ItemId)),
            _cacheService.RemoveFromSet(CacheKeys.UsedItem.GetUsedItemKey(notification.ItemId), notification.TradeId)
        );
    }
}
