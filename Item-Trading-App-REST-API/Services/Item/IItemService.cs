using Item_Trading_App_REST_API.Models.Item;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Item;

public interface IItemService
{
    /// <summary>
    /// Creates a new item
    /// </summary>
    Task<FullItemResult> CreateItemAsync(CreateItem model);

    /// <summary>
    /// Updates the item
    /// </summary>
    Task<FullItemResult> UpdateItemAsync(UpdateItem model);

    /// <summary>
    /// Deletes an item
    /// </summary>
    Task<DeleteItemResult> DeleteItemAsync(string itemId, string senderUserId);

    /// <summary>
    /// Returns details about an item
    /// </summary>
    Task<FullItemResult> GetItemAsync(string itemId);

    /// <summary>
    /// Enlists all the current items
    /// </summary>
    Task<ItemsResult> ListItemsAsync(string searchString = "");

    /// <summary>
    /// Returns the item's name
    /// </summary>
    Task<string> GetItemNameAsync(string itemId);

    /// <summary>
    /// Returns the item's description
    /// </summary>
    Task<string> GetItemDescriptionAsync(string itemId);
}
