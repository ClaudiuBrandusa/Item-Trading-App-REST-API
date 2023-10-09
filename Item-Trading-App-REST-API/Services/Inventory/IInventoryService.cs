using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory
{
    public interface IInventoryService
    {
        /// <summary>
        /// Tells if the user has the quantity of the item
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task<bool> HasItemAsync(string userId, string itemId, int quantity);

        /// <summary>
        /// Adds the quantity of the item to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task<QuantifiedItemResult> AddItemAsync(string userId, string itemId, int quantity, bool notify = false);

        /// <summary>
        /// Drops the quantity of the item from the inventory of the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task<QuantifiedItemResult> DropItemAsync(string userId, string itemId, int quantity, bool notify = false);

        /// <summary>
        /// Returns the quantity of the item of the given user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<QuantifiedItemResult> GetItemAsync(string userId, string itemId);

        /// <summary>
        /// Returns a list of all items from the user's inventory
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<ItemsResult> ListItemsAsync(string userId, string searchString = "");

        /// <summary>
        /// Locks a given quantity of the item from the user's inventory
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task<LockItemResult> LockItemAsync(string userId, string itemId, int quantity, bool notify = false);

        /// <summary>
        /// Unlocks a given quantity of the item from the user's inventory
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task<LockItemResult> UnlockItemAsync(string userId, string itemId, int quantity, bool notify = false);

        /// <summary>
        /// Returns the amount of the item with the given itemId for the user with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<LockedItemAmountResult> GetLockedAmount(string userId, string itemId);

        /// <summary>
        /// Returns the user ids that own the item with the given itemId
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<OwnedItemByUsers> GetUsersThatOwnThisItem(string itemId);

        /// <summary>
        /// Sends the notifications and clears the cached used by the deleted item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task RemoveItem(List<string> userIds, string itemId);
    }
}
