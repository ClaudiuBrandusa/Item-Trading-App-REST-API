using Application.Models.TradeItems;
using Application.Models.Trades;
using Domain.Aggregates.Trades;
using Domain.Entities.Trades;

namespace Application.Repositories;

public interface ICachedTradeRepository : ICachedRepository, IDisposable
{
    /// <param name="tradeId"></param>
    /// <param name="responded"></param>
    /// <returns>Trade's items</returns>
    Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded);

    Task<CachedTrade?> GetCachedTradeAsync(string tradeId);

    Task<string[]> ListReceivedTradeIdsCachedAsync(string userId);

    Task<string[]> ListSentTradeIdsCachedAsync(string userId);

    Task SetCacheForTrade(Trade trade, string senderId, string receiverId, TradeItemDTO[] tradeItemIds);

    Task ClearTradeCache(string tradeId, string senderId, string receiverId, string[] tradeItemIds);
}
