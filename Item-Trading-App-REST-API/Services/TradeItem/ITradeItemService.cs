using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.TradeItem;

public interface ITradeItemService
{
    /// <summary>
    /// Adds a new trade item
    /// </summary>
    Task<bool> AddTradeItemAsync(AddTradeItemCommand model);


    /// <summary>
    /// Returns the trade items of the given trade id as an array
    /// </summary>
    Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsQuery model);

    /// <summary>
    /// Returns an array of the trade ids that contain the item with the given id
    /// </summary>
    Task<string[]> GetItemTradeIdsAsync(ItemUsedInTradeQuery model);
}
