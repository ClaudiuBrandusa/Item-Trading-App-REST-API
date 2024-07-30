using Application.Behaviors.TradeItem.AddTradeItem;
using Application.Behaviors.TradeItem.GetTradeItemIds;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItem.HasTradeItem;
using Application.Behaviors.TradeItem.RemoveTradeItems;
using Domain.Entities.Trades;

namespace Application.Services.TradeItems;

public interface ITradeItemService
{
    /// <summary>
    /// Adds a new trade item
    /// </summary>
    Task<bool> AddTradeItemAsync(AddTradeItemCommand model);

    /// <summary>
    /// Checks if the trade has the item with the given id
    /// </summary>
    Task<bool> HasTradeItemAsync(HasTradeItemQuery model);

    /// <summary>
    /// Returns the trade items of the given trade id as an array
    /// </summary>
    Task<TradeItem[]> GetTradeItemAsync(GetTradeItemsQuery model);

    /// <summary>
    /// Returns an array of the trade ids that contain the item with the given id
    /// </summary>
    Task<string[]> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model);

    /// <summary>
    /// Removes the trade items from a trade and returns the result of the operation
    /// </summary>
    Task<bool> RemoveTradeItemsAsync(RemoveTradeItemsCommand model);
}
