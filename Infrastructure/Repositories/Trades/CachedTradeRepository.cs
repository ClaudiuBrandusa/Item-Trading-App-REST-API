using Application.Constants;
using Application.Services.Cache;
using Domain.Repositories.Trades;
using Domain.ValueObjects.Trades;
using Application.Extensions;
using Domain.Aggregates.Trades;
using Domain.Entities.Trades;

namespace Infrastructure.Repositories.Trades;

public class CachedTradeRepository : CachedRepository, ICachedTradeRepository
{
    private readonly ITradeRepository _repository;

    public CachedTradeRepository(ITradeRepository repository, ICacheService cacheService) : base(repository, cacheService)
    {
        _repository = repository;
    }

    public ValueTask AddSentAndReceivedTradeEntitiesAsync(string tradeId, string senderUserId, string receiverUserId) =>
        _repository.AddSentAndReceivedTradeEntitiesAsync(tradeId, senderUserId, receiverUserId);

    public Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded) => _repository.GetTradeItemsAsync(tradeId, responded);

    public Task<CachedTrade?> GetCachedTradeAsync(string tradeId)
    {
        return _cacheService.GetEntityReferenceAsync(
            CacheKeys.Trade.GetTradeKey(tradeId),
            async (args) =>
            {
                var tradeTask = _repository.GetTradeEntityAsync(tradeId);
                var sentTradeTask = _repository.GetSentTradeEntityAsync(tradeId);
                var receivedTradeTask = _repository.GetReceivedTradeEntityAsync(tradeId);

                var trade = await tradeTask;

                if (trade is null) return null;

                var tradeItems = await _repository.GetTradeItemsAsync(trade.TradeId, trade.Response.HasValue /* if trade.Response has value, then it means it is a responded trade */ );

                return new CachedTrade(
                    trade.TradeId,
                    (await sentTradeTask)?.SenderId ?? "",
                    (await receivedTradeTask)?.ReceiverId ?? "",
                    trade.SentDate,
                    trade.Response,
                    trade.ResponseDate,
                    tradeItems
                );
            },
            true
        );
    }

    public Task<string[]> ListReceivedTradeIdsCachedAsync(string userId)
    {
        return _cacheService.GetEntityIdsAsync(
            CacheKeys.Trade.GetReceivedTradeKey(userId, ""),
            async (args) => await _repository.ListReceivedTradeIdsAsync(userId),
            true
        );
    }

    public Task<string[]> ListSentTradeIdsCachedAsync(string userId)
    {
        return _cacheService.GetEntityIdsAsync(
            CacheKeys.Trade.GetSentTradeKey(userId, ""),
            async (args) => await _repository.ListSentTradeIdsAsync(userId),
            true
        );
    }

    public Task SetCacheForTrade(Trade trade, string senderId, string receiverId, TradeItem[] tradeItemIds)
    {
        string tradeId = trade.TradeId;

        return Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(tradeId), new CachedTrade(
                tradeId,
                senderId,
                receiverId,
                trade.SentDate,
                trade.Response,
                trade.ResponseDate,
                tradeItemIds
            )),
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetSentTradeKey(senderId, tradeId), ""),
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetReceivedTradeKey(receiverId, tradeId), "")
        );
    }

    public Task ClearTradeCache(string tradeId, string senderId, string receiverId, string[] tradeItemIds)
    {
        var tasks = new Task[3 + tradeItemIds.Length];

        tasks[0] = _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetTradeKey(tradeId));
        tasks[1] = _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetSentTradeKey(senderId, tradeId));
        tasks[2] = _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetReceivedTradeKey(receiverId, tradeId));
        for (int i = 0; i < tradeItemIds.Length; i++)
            tasks[3 + i] = _cacheService.RemoveFromSet(CacheKeys.UsedItem.GetUsedItemKey(tradeItemIds[i]), tradeId);

        return Task.WhenAll(tasks);
    }

    public void Dispose() => _repository.Dispose();

    protected override string GetCacheKey(object entity)
    {
        if (entity is not Trade trade) return string.Empty;

        return CacheKeys.Item.GetItemKey(trade.TradeId);
    }
}
