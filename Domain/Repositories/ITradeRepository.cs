using Domain.Trade;
using Domain.TradeItems;

namespace Domain.Repositories;

public interface ITradeRepository : IRepository, IDisposable
{
    Task<CachedTrade> GetCachedTradeAsync(string tradeId);

    ValueTask AddSentAndReceivedTradeEntitiesAsync(string tradeId, string senderUserId, string receiverUserId);

    Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded);

    Task<Trades.Trade?> GetTradeEntityAsync(string tradeId);

    Task<Trades.SentTrade?> GetSentTradeEntityAsync(string tradeId);

    Task<Trades.ReceivedTrade?> GetReceivedTradeEntityAsync(string tradeId);

    Task<string[]> ListReceivedTradeIdsAsync(string userId);

    Task<string[]> ListSentTradeIdsAsync(string userId);

    Task<string[]> ListReceivedTradeIdsCachedAsync(string userId);

    Task<string[]> ListSentTradeIdsCachedAsync(string userId);
}
