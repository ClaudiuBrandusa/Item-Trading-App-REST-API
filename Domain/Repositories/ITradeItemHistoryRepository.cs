using Domain.TradeItems;
using Domain.Trades;

namespace Domain.Repositories;

public interface ITradeItemHistoryRepository : IRepository, IDisposable
{
    Task<bool> AddTradeItemHistoryAsync(string tradeId, TradeItem tradeItem);

    Task<TradeItem[]> ListTradeContentHistoryAsTradeItemCachedAsync(string tradeId);

    Task<TradeContentHistory[]> ListTradeContentHistoryAsync(string tradeId);

    Task<int> DeleteTradeContentHistoryForTradeAsync(string tradeId);
}
