using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.TradeItem;

public interface ITradeItemService
{
    /// <summary>
    /// Adds a new trade item
    /// </summary>
    Task<bool> AddTradeItemAsync(AddTradeItemCommand model);

    /// <summary>
    /// Returns a list of the trade items for the given trade id
    /// </summary>
    Task<List<Models.TradeItems.TradeItem>> GetTradeItemsAsync(GetTradeItemsQuery model);

    /// <summary>
    /// Returns a list of the item prices for the given trade id
    /// </summary>
    Task<List<ItemPrice>> GetItemPricesAsync(GetItemPricesQuery model);

    /// <summary>
    /// Returns a list of the trade ids that contain the item with the given id
    /// </summary>
    Task<List<string>> GetItemTradeIdsAsync(ItemUsedInTradeQuery model);
}
