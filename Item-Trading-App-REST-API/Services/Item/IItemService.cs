using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Item;

public interface IItemService
{
    /// <summary>
    /// Creates a new item
    /// </summary>
    Task<FullItemResult> CreateItemAsync(CreateItemCommand model);

    /// <summary>
    /// Updates the item
    /// </summary>
    Task<FullItemResult> UpdateItemAsync(UpdateItemCommand model);

    /// <summary>
    /// Deletes an item
    /// </summary>
    Task<DeleteItemResult> DeleteItemAsync(DeleteItemCommand model);

    /// <summary>
    /// Returns details about an item
    /// </summary>
    Task<FullItemResult> GetItemAsync(GetItemQuery model);

    /// <summary>
    /// Enlists all the current items
    /// </summary>
    Task<ItemsResult> ListItemsAsync(ListItemsQuery model);

    /// <summary>
    /// Returns the item's name
    /// </summary>
    Task<string> GetItemNameAsync(GetItemNameQuery model);

    /// <summary>
    /// Returns the item's description
    /// </summary>
    Task<string> GetItemDescriptionAsync(GetItemDescriptionQuery model);
}
