using Application.Constants;
using Application.Services.Cache;
using Domain.DomainEvents.Trades;
using MediatR;

namespace Application.Behaviors.TradeItem.RemoveTradeItems;

public class TradeItemRemovedEventHandler : INotificationHandler<TradeItemRemovedDomainEvent>
{
    private readonly ICacheService _cacheService;

    public TradeItemRemovedEventHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task Handle(TradeItemRemovedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.KeepCache)
        {
            return _cacheService.ClearCacheKeysStartingWith(CacheKeys.TradeItem.GetTradeItemKey(notification.TradeId, string.Empty));
        }

        return Task.CompletedTask;
    }
}
