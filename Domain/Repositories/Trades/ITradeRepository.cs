using Domain.Aggregates.Trades;
using Domain.Entities.Trades;

namespace Domain.Repositories.Trades;

public interface ITradeRepository : IRepository, IDisposable
{
    Task<Trade?> GetTradeAsync(string tradeId);

    Task<bool?> GetTradeResponseAsync(string tradeId);

    Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded);

    Task<Trade?> GetTradeEntityAsync(string tradeId);

    Task<SentTrade?> GetSentTradeEntityAsync(string tradeId);

    Task<ReceivedTrade?> GetReceivedTradeEntityAsync(string tradeId);

    Task<string[]> ListReceivedTradeIdsAsync(string userId);

    Task<string[]> ListSentTradeIdsAsync(string userId);
}
