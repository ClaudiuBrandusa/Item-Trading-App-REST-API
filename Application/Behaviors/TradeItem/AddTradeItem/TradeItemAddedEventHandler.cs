using Application.Constants;
using Application.Services.Cache;
using Domain.DomainEvents.Trades;
using MediatR;

namespace Application.Behaviors.TradeItem.AddTradeItem;

public class TradeItemAddedEventHandler : INotificationHandler<TradeItemAddedDomainEvent>
{
    private readonly ICacheService _cacheService;

    public TradeItemAddedEventHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task Handle(TradeItemAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.TradeItem.GetTradeItemKey(notification.TradeId, notification.Data.ItemId), notification.Data),
            _cacheService.AddToSet(CacheKeys.UsedItem.GetUsedItemKey(notification.Data.ItemId), notification.TradeId)
        );
    }
}
