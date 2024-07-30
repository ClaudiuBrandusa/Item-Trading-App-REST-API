using Domain.Entities.Trades;

namespace Domain.Repositories.TradeItemsHistory;

public interface ICachedTradeItemHistoryRepository : IRepository, IDisposable
{
    Task<bool> AddTradeItemHistoryAsync(string tradeId, string itemName, TradeItem tradeItem);

    Task<TradeItemHistory[]> ListTradeItemHistoryAsync(string tradeId);

    Task<int> DeleteTradeItemHistoryForTradeAsync(string tradeId);
}
