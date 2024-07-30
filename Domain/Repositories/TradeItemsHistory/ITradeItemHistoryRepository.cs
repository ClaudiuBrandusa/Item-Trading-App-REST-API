using Domain.Entities.Trades;

namespace Domain.Repositories.TradeItemsHistory;

public interface ITradeItemHistoryRepository : IRepository, IDisposable
{
    Task<bool> AddTradeItemHistoryAsync(string tradeId, string itemName, TradeItem tradeItem);

    Task<TradeItemHistory[]> ListTradeItemsHistoryAsync(string tradeId);

    Task<int> DeleteTradeItemsHistoryForTradeAsync(string tradeId);
}
