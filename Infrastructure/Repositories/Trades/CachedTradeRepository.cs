using Application.Constants;
using Application.Services.Cache;
using Domain.Repositories.Trades;
using Application.Extensions;
using Domain.Aggregates.Trades;
using Domain.Entities.Trades;
using Application.Repositories;
using Application.Models.Trades;

namespace Infrastructure.Repositories.Trades;

public class CachedTradeRepository : CachedRepository, ICachedTradeRepository
{
    private readonly ITradeRepository _repository;

    public CachedTradeRepository(ITradeRepository repository, ICacheService cacheService) : base(repository, cacheService)
    {
        _repository = repository;
    }

    public async Task<Trade?> GetTradeAsync(string tradeId)
    {
        var trade = await _repository.GetTradeAsync(tradeId);

        return trade;
    }

    public async Task<bool?> GetTradeResponseAsync(string tradeId)
    {
        var response = await _cacheService.GetCacheValueAsync<bool?>(
            GetTradeResponseCacheKey(tradeId)
        );

        if (response is null)
        {
            response = await _repository.GetTradeResponseAsync(tradeId);

            if (response is null)
            {
                return null;
            }

            await SetTradeResponseCacheAsync(tradeId, response.Value);
        }

        return response;
    }

    public Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded) => _repository.GetTradeItemsAsync(tradeId, responded);

    public Task<string[]> GetTradeIdsUsingItemAsync(string itemId)
    {
        return _cacheService.GetSetValuesAsync(CacheKeys.UsedItem.GetUsedItemKey(itemId), async (args) =>
        {
            return await _repository.GetTradeIdsUsingItemAsync(itemId);
        },
        true);
    }

    public async Task<bool> HasTradeItem(string tradeId, string itemId, bool responded = false)
    {
        if (responded)
        {
            return await _repository.HasTradeItemHistoryAsync(tradeId, itemId);
        }

        var cacheKey = CacheKeys.TradeItem.GetTradeItemKey(tradeId, itemId);
        
        var keyExist = await _cacheService.ContainsKey(cacheKey);

        if (keyExist)
            return true;
        
        return await _repository.HasTradeItemAsync(tradeId, itemId);
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

    public Task SetCacheForTrade(Trade trade)
    {
        string tradeId = trade.TradeId;
        string senderId = trade.GetSenderId();
        string receiverId = trade.GetReceiverId();

        return Task.WhenAll(
            _cacheService.SetCacheValueAsync(GetTradeCacheKey(tradeId), new CachedTrade(
                tradeId,
                senderId,
                receiverId,
                trade.SentDate,
                trade.Response,
                trade.ResponseDate,
                trade.TradeContents.ToArray()
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

    public async Task<bool> IsItemUsedInTrade(string itemId)
    {
        return await _repository.IsItemUsedInTrade(itemId);
    }

    public async Task<bool> MoveTradeContentToHistory(string tradeId)
    {
        return await _repository.MoveTradeContentToHistory(tradeId);
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

    private async Task SetTradeResponseCacheAsync(string tradeId, bool response)
    {
        await _cacheService.SetCacheValueAsync(
            GetTradeResponseCacheKey(tradeId),
            response
        );
    }

    private static string GetTradeCacheKey(string tradeId) => CacheKeys.Trade.GetTradeKey(tradeId);

    private static string GetTradeResponseCacheKey(string tradeId) => CacheKeys.Trade.GetTradeResponseCacheKey(tradeId);

    private static string GetSentTradeCacheKey(string tradeId, string senderId) => CacheKeys.Trade.GetSentTradeKey(senderId, tradeId);

    private static string GetReceivedTradeCacheKey(string tradeId, string receiverId) => CacheKeys.Trade.GetReceivedTradeKey(receiverId, tradeId);
}
