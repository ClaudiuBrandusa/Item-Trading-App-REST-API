using Domain.Aggregates.Trades;
using Domain.Entities.Trades;

namespace Domain.Repositories.Trades;

public interface ITradeRepository : IRepository, IDisposable
{
    Task<Trade?> GetTradeAsync(string tradeId);

    Task<bool?> GetTradeResponseAsync(string tradeId);

    Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded);

    Task<Trade?> GetTradeEntityAsync(string tradeId);

    Task<string[]> GetTradeIdsUsingItemAsync(string itemId);

    Task<string[]> ListReceivedTradeIdsAsync(string userId);

    Task<string[]> ListSentTradeIdsAsync(string userId);

    Task<bool> HasTradeItemAsync(string tradeId, string itemId);

    Task<bool> HasTradeItemHistoryAsync(string tradeId, string itemId);

    Task<bool> IsItemUsedInTrade(string itemId);

    Task<bool> MoveTradeContentToHistory(string tradeId);
}
