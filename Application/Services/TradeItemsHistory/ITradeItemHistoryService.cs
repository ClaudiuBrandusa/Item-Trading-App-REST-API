using Application.Behaviors.TradeItemHistory.AddTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Application.Behaviors.TradeItemHistory.RemoveTradeItems;
using Application.Results.TradeItemsHistory;
using Domain.Entities.Trades;

namespace Application.Services.TradeItemsHistory;

/// <summary>
/// Handles the trade contents of the responded trades
/// </summary>
public interface ITradeItemHistoryService
{
    /// <summary>
    /// Adds trade items to a responded trade
    /// </summary>
    Task<TradeItemHistoryResult> AddTradeItemsAsync(AddTradeItemsHistoryCommand model);

    /// <summary>
    /// Returns the trade items of a responded trade
    /// </summary>
    Task<TradeItem[]> GetTradeItemsAsync(GetTradeItemsHistoryQuery model);

    /// <summary>
    /// Removes the trade items of a responded trade
    /// </summary>
    Task<TradeItemHistoryResult> RemoveTradeItemsAsync(RemoveTradeItemsHistoryCommand model);
}
