using Application.Behaviors.Inventory.AddItem;
using Application.Behaviors.Inventory.DropItem;
using Application.Behaviors.Inventory.GetItem;
using Application.Behaviors.Inventory.GetLockedAmount;
using Application.Behaviors.Inventory.HasItem;
using Application.Behaviors.Inventory.ListItems;
using Application.Behaviors.Inventory.ListUsersOwningItem;
using Application.Behaviors.Inventory.LockItem;
using Application.Behaviors.Inventory.RemoveItemFromUsers;
using Application.Behaviors.Inventory.UnlockItem;
using Application.Models.Inventory;
using Application.Results.Inventory;
using Application.Results.Items;

namespace Application.Services.Inventory;

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
    Task<LockedItemAmountResult> GetLockedAmountAsync(GetInventoryItemLockedAmountQuery model);

    /// <summary>
    /// Returns the user ids that own the item with the given itemId
    /// </summary>
    Task<UsersOwningItem> GetUsersOwningThisItemAsync(GetUserIdsOwningItemQuery model);

    /// <summary>
    /// Sends the notifications and clears the cached used by the deleted item
    /// </summary>
    Task RemoveItemCacheAsync(RemoveItemFromUsersCommand model);
}
