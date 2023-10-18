using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory;

public interface IInventoryService
{
    /// <summary>
    /// Tells if the user has the quantity of the item
    /// </summary>
    Task<bool> HasItemAsync(HasItem model);

    /// <summary>
    /// Adds the quantity of the item to the user
    /// </summary>
    Task<QuantifiedItemResult> AddItemAsync(AddItem model, bool notify = false);

    /// <summary>
    /// Drops the quantity of the item from the inventory of the user
    /// </summary>
    Task<QuantifiedItemResult> DropItemAsync(DropItem model, bool notify = false);

    /// <summary>
    /// Returns the quantity of the item of the given user
    /// </summary>
    Task<QuantifiedItemResult> GetItemAsync(GetUsersItem model);

    /// <summary>
    /// Returns a list of all items from the user's inventory
    /// </summary>
    Task<ItemsResult> ListItemsAsync(ListItems model);

    /// <summary>
    /// Locks a given quantity of the item from the user's inventory
    /// </summary>
    Task<LockItemResult> LockItemAsync(LockInventoryItem model, bool notify = false);

    /// <summary>
    /// Unlocks a given quantity of the item from the user's inventory
    /// </summary>
    Task<LockItemResult> UnlockItemAsync(LockInventoryItem model, bool notify = false);

    /// <summary>
    /// Returns the amount of the item with the given itemId for the user with the given id
    /// </summary>
    Task<LockedItemAmountResult> GetLockedAmount(GetUsersItem model);

    /// <summary>
    /// Returns the user ids that own the item with the given itemId
    /// </summary>
    Task<OwnedItemByUsers> GetUsersThatOwnThisItem(string itemId);

    /// <summary>
    /// Sends the notifications and clears the cached used by the deleted item
    /// </summary>
    Task RemoveItemCacheAsync(RemoveItemFromUsers model);
}
