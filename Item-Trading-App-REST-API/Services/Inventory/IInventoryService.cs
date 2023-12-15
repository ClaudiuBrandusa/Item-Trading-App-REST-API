using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory;

public interface IInventoryService
{
    /// <summary>
    /// Tells if the user has the quantity of the item
    /// </summary>
    Task<bool> HasItemAsync(HasItemQuantityQuery model);

    /// <summary>
    /// Adds the quantity of the item to the user
    /// </summary>
    Task<QuantifiedItemResult> AddItemAsync(AddInventoryItemCommand model);

    /// <summary>
    /// Drops the quantity of the item from the inventory of the user
    /// </summary>
    Task<QuantifiedItemResult> DropItemAsync(DropInventoryItemCommand model);

    /// <summary>
    /// Returns the quantity of the item of the given user
    /// </summary>
    Task<QuantifiedItemResult> GetItemAsync(GetInventoryItemQuery model);

    /// <summary>
    /// Returns a list of all items from the user's inventory
    /// </summary>
    Task<ItemsResult> ListItemsAsync(ListInventoryItemsQuery model);

    /// <summary>
    /// Locks a given quantity of the item from the user's inventory
    /// </summary>
    Task<LockItemResult> LockItemAsync(LockItemCommand model);

    /// <summary>
    /// Unlocks a given quantity of the item from the user's inventory
    /// </summary>
    Task<LockItemResult> UnlockItemAsync(UnlockItemCommand model);

    /// <summary>
    /// Returns the amount of the item with the given itemId for the user with the given id
    /// </summary>
    Task<LockedItemAmountResult> GetLockedAmount(GetInventoryItemLockedAmountQuery model);

    /// <summary>
    /// Returns the user ids that own the item with the given itemId
    /// </summary>
    Task<UsersOwningItem> GetUsersOwningThisItem(GetUserIdsOwningItemQuery model);

    /// <summary>
    /// Sends the notifications and clears the cached used by the deleted item
    /// </summary>
    Task RemoveItemCacheAsync(RemoveItemFromUsersCommand model);
}
