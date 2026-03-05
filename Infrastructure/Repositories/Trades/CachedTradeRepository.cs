using Application.Constants;
using Application.Services.Cache;
using Domain.Repositories.Trades;
using Application.Extensions;
using Domain.Aggregates.Trades;
using Domain.Entities.Trades;
using Application.Repositories;
using Application.Models.TradeItems;
using Application.Models.Trades;
using MediatR;
using Application.Behaviors.Item.GetItemName;

namespace Infrastructure.Repositories.Trades;

public class CachedTradeRepository : CachedRepository, ICachedTradeRepository
{
    private readonly ITradeRepository _repository;
    private readonly ISender _sender;

    public CachedTradeRepository(ITradeRepository repository, ICacheService cacheService, ISender sender) : base(repository, cacheService)
    {
        _repository = repository;
        _sender = sender;
    }

    public Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded) => _repository.GetTradeItemsAsync(tradeId, responded);

    public Task<CachedTrade?> GetCachedTradeAsync(string tradeId)
    {
        return _cacheService.GetEntityReferenceAsync(
            GetTradeCacheKey(tradeId),
            async (args) =>
            {
                var tradeTask = _repository.GetTradeEntityAsync(tradeId);
                var sentTradeTask = _repository.GetSentTradeEntityAsync(tradeId);
                var receivedTradeTask = _repository.GetReceivedTradeEntityAsync(tradeId);

                var trade = await tradeTask;

                if (trade is null) return null;

                var tradeItems = await _repository.GetTradeItemsAsync(trade.TradeId, trade.Response.HasValue /* if trade.Response has value, then it means it is a responded trade */ );

                var tradeItemDTOs = tradeItems.Select(x => new TradeItemDTO
                {
                    ItemId = x.ItemId,
                    Price = x.Price,
                    Quantity = x.Quantity
                }).ToArray();

                var tasks = new List<Task>();

                for (int i = 0; i < tradeItems.Length; i++)
                {
                    var tradeItem = tradeItemDTOs[i];

                    var task = Task.Factory.StartNew(async () =>
                    {
                        var query = new GetItemNameQuery
                        {
                            ItemId = tradeItem.ItemId
                        };

                        var itemName = await _sender.Send(query);

                        tradeItem.ItemName = itemName;
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                return new CachedTrade(
                    trade.TradeId,
                    (await sentTradeTask)?.SenderId ?? "",
                    (await receivedTradeTask)?.ReceiverId ?? "",
                    trade.SentDate,
                    trade.Response,
                    trade.ResponseDate,
                    tradeItemDTOs
                );
            },
            true
        );
    }

    public Task<string[]> ListReceivedTradeIdsCachedAsync(string userId)
    {
        return _cacheService.GetEntityIdsAsync(
            GetReceivedTradeCacheKey("", userId),
            async (args) => await _repository.ListReceivedTradeIdsAsync(userId),
            true
        );
    }

    public Task<string[]> ListSentTradeIdsCachedAsync(string userId)
    {
        return _cacheService.GetEntityIdsAsync(
            GetSentTradeCacheKey("", userId),
            async (args) => await _repository.ListSentTradeIdsAsync(userId),
            true
        );
    }

    public Task SetCacheForTrade(Trade trade, string senderId, string receiverId, TradeItemDTO[] tradeItems)
    {
        string tradeId = trade.TradeId;

        return Task.WhenAll(
            _cacheService.SetCacheValueAsync(GetTradeCacheKey(tradeId), new CachedTrade(
                tradeId,
                senderId,
                receiverId,
                trade.SentDate,
                trade.Response,
                trade.ResponseDate,
                tradeItems
            )),
            _cacheService.SetCacheValueAsync(GetSentTradeCacheKey(tradeId, senderId), ""),
            _cacheService.SetCacheValueAsync(GetReceivedTradeCacheKey(tradeId, receiverId), "")
        );
    }

    public Task ClearTradeCache(string tradeId, string senderId, string receiverId, string[] tradeItemIds)
    {
        var tasks = new Task[3 + tradeItemIds.Length];

        tasks[0] = _cacheService.ClearCacheKeyAsync(GetTradeCacheKey(tradeId));
        tasks[1] = _cacheService.ClearCacheKeyAsync(GetSentTradeCacheKey(tradeId, senderId));
        tasks[2] = _cacheService.ClearCacheKeyAsync(GetReceivedTradeCacheKey(tradeId, receiverId));
        for (int i = 0; i < tradeItemIds.Length; i++)
            tasks[3 + i] = _cacheService.RemoveFromSet(CacheKeys.UsedItem.GetUsedItemKey(tradeItemIds[i]), tradeId);

        return Task.WhenAll(tasks);
    }

    public void Dispose() => _repository.Dispose();

    protected override string GetCacheKey(object entity)
    {
        if (entity is not Trade trade) return string.Empty;

        return GetTradeCacheKey(trade.TradeId);
    }

    protected override Task SetCacheAsync(object entity)
    {
        // the cache will be set from a separate method
        return Task.CompletedTask;
    }

    private static string GetTradeCacheKey(string tradeId) => CacheKeys.Trade.GetTradeKey(tradeId);

    private static string GetSentTradeCacheKey(string tradeId, string senderId) => CacheKeys.Trade.GetSentTradeKey(senderId, tradeId);

    private static string GetReceivedTradeCacheKey(string tradeId, string receiverId) => CacheKeys.Trade.GetReceivedTradeKey(receiverId, tradeId);
}
