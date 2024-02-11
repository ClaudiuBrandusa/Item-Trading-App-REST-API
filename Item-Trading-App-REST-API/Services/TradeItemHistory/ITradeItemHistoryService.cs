using Item_Trading_App_REST_API.Models.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Queries.TradeItemHistory;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.TradeItemHistory;

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
    Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsHistoryQuery model);

    /// <summary>
    /// Removes the trade items of a responded trade
    /// </summary>
    Task<TradeItemHistoryBaseResult> RemoveTradeItemsAsync(RemoveTradeItemsHistoryCommand model);
}
