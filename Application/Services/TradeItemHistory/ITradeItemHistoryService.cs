using Application.Behaviors.TradeItemHistory.AddTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Application.Behaviors.TradeItemHistory.RemoveTradeItems;
using Application.Models.TradeItemHistory;

namespace Application.Services.TradeItemHistory;

/// <summary>
/// Handles the trade contents of the responded trades
/// </summary>
public interface ITradeItemHistoryService
{
    /// <summary>
    /// Adds trade items to a responded trade
    /// </summary>
    Task<TradeItemHistoryBaseResult> AddTradeItemsAsync(AddTradeItemsHistoryCommand model);

    /// <summary>
    /// Returns the trade items of a responded trade
    /// </summary>
    Task<Domain.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsHistoryQuery model);

    /// <summary>
    /// Removes the trade items of a responded trade
    /// </summary>
    Task<TradeItemHistoryBaseResult> RemoveTradeItemsAsync(RemoveTradeItemsHistoryCommand model);
}
