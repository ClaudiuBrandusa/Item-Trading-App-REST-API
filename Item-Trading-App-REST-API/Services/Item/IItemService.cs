using Item_Trading_App_REST_API.Models.Item;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Item
{
    public interface IItemService
    {
        /// <summary>
        /// Creates a new item
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<FullItemResult> CreateItemAsync(CreateItem model);

        /// <summary>
        /// Updates the item
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<FullItemResult> UpdateItemAsync(UpdateItem model);

        /// <summary>
        /// Deletes an item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></retu'rns>
        Task<DeleteItemResult> DeleteItemAsync(string itemId, string senderUserId);

        /// <summary>
        /// Returns details about an item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<FullItemResult> GetItemAsync(string itemId);

        /// <summary>
        /// Enlists all the current items
        /// </summary>
        /// <param name="searchString">Will enlist the items' with the name starting with the search string</param>
        /// <returns></returns>
        Task<ItemsResult> ListItems(string searchString = "");

        /// <summary>
        /// Returns the item's name
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<string> GetItemNameAsync(string itemId);

        /// <summary>
        /// Returns the item's description
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<string> GetItemDescriptionAsync(string itemId);
    }
}
