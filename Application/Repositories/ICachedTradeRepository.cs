using Application.Models.Trades;
using Domain.Aggregates.Trades;
using Domain.Entities.Trades;

namespace Application.Repositories;

public interface ICachedTradeRepository : ICachedRepository, IDisposable
{
    Task<Trade?> GetTradeAsync(string tradeId);

    /// <param name="tradeId"></param>
    /// <param name="responded"></param>
    /// <returns>Trade's items</returns>
    Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded);

    /// <summary>
    /// Returns the response of the trade. Will return null if the trade was not yet responded.
    /// </summary>
    Task<bool?> GetTradeResponseAsync(string tradeId);

    Task<CachedTrade?> GetCachedTradeAsync(string tradeId);

    Task<string[]> GetTradeIdsUsingItemAsync(string itemId);

    Task<bool> HasTradeItem(string tradeId, string itemId, bool responded = false);

    Task<string[]> ListReceivedTradeIdsCachedAsync(string userId);

    Task<string[]> ListSentTradeIdsCachedAsync(string userId);

    Task SetCacheForTrade(Trade trade);

    Task ClearTradeCache(string tradeId, string senderId, string receiverId, string[] tradeItemIds);

    Task<bool> IsItemUsedInTrade(string itemId);

    Task<bool> MoveTradeContentToHistory(string tradeId);
}
