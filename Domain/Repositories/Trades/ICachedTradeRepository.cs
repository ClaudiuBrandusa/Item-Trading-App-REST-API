using Domain.Aggregates.Trades;
using Domain.Entities.Trades;
using Domain.ValueObjects.Trades;

namespace Domain.Repositories.Trades;

public interface ICachedTradeRepository : ICachedRepository, IDisposable
{
    ValueTask AddSentAndReceivedTradeEntitiesAsync(string tradeId, string senderUserId, string receiverUserId);

    /// <param name="tradeId"></param>
    /// <param name="responded"></param>
    /// <returns>Trade's items</returns>
    Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded);

    Task<CachedTrade?> GetCachedTradeAsync(string tradeId);

    Task<string[]> ListReceivedTradeIdsCachedAsync(string userId);

    Task<string[]> ListSentTradeIdsCachedAsync(string userId);

    Task SetCacheForTrade(Trade trade, string senderId, string receiverId, TradeItem[] tradeItemIds);

    Task ClearTradeCache(string tradeId, string senderId, string receiverId, string[] tradeItemIds);
}
