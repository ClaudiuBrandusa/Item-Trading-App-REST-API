using Application.Constants;
using Application.Services.Cache;
using MediatR;

namespace Application.Behaviors.TradeItem.RemoveTradeItems;

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
            return _cacheService.ClearCacheKeysStartingWith(CacheKeys.TradeItem.GetTradeItemKey(notification.TradeId, string.Empty));
        }

        return Task.CompletedTask;
    }
}
