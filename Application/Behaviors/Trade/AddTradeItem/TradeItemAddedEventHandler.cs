using Application.Constants;
using Application.Services.Cache;
using Domain.DomainEvents.Trades;
using MediatR;

namespace Application.Behaviors.Trade.AddTradeItem;

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
            _cacheService.SetCacheValueAsync(CacheKeys.TradeItem.GetTradeItemKey(notification.TradeId, notification.ItemId), new
            {
                notification.TradeId,
                notification.ItemId,
                notification.Quantity,
                notification.Price
            }),
            _cacheService.AddToSet(CacheKeys.UsedItem.GetUsedItemKey(notification.ItemId), notification.TradeId)
        );
    }
}
