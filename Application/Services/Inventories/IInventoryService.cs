using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.GetItem;
using Application.Behaviors.Inventories.GetLockedAmount;
using Application.Behaviors.Inventories.HasItem;
using Application.Behaviors.Inventories.ListItems;
using Application.Behaviors.Inventories.ListUsersOwningItem;
using Application.Behaviors.Inventories.LockItem;
using Application.Behaviors.Inventories.LockItems;
using Application.Behaviors.Inventories.RemoveItemFromUsers;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Models.Inventories;
using Application.Results.Inventories;
using Application.Results.Items;

namespace Application.Services.Inventories;

public interface IInventoryService
{
    /// <summary>
    /// Tells if the user has the quantity of the item
    /// </summary>
    Task<bool> HasItemAsync(HasItemQuantityQuery model);

    /// <summary>
    /// Adds the quantity of the item to the user
    /// </summary>
    Task<Result<QuantifiedItemResult>> AddItemAsync(AddInventoryItemCommand model);

    /// <summary>
    /// Drops the quantity of the item from the inventory of the user
    /// </summary>
    Task<Result<QuantifiedItemResult>> DropItemAsync(DropInventoryItemCommand model);

    /// <summary>
    /// Returns the quantity of the item of the given user
    /// </summary>
    Task<Result<QuantifiedItemResult>> GetItemAsync(GetInventoryItemQuery model);

    /// <summary>
    /// Returns a list of all items from the user's inventory
    /// </summary>
    Task<Result<ItemsResult>> ListItemsAsync(ListInventoryItemsQuery model);

    /// <summary>
    /// Locks a given quantity of the item from the user's inventory
    /// </summary>
    Task<Result<LockItemResult>> LockItemAsync(LockItemCommand model);

    /// <summary>
    /// Locks a given set of items from the user's inventory
    /// </summary>
    Task<Result<LockItemsResult>> LockItemsAsync(LockItemsCommand model);

    /// <summary>
    /// Unlocks a given quantity of the item from the user's inventory
    /// </summary>
    Task<Result<LockItemResult>> UnlockItemAsync(UnlockItemCommand model);

    /// <summary>
    /// Returns the amount of the item with the given itemId for the user with the given id
    /// </summary>
    Task<Result<LockedItemAmountResult>> GetLockedAmountAsync(GetInventoryItemLockedAmountQuery model);

    /// <summary>
    /// Returns the user ids that own the item with the given itemId
    /// </summary>
    Task<Result<UsersOwningItem>> GetUsersOwningThisItemAsync(GetUserIdsOwningItemQuery model);

    /// <summary>
    /// Sends the notifications and clears the cached used by the deleted item
    /// </summary>
    Task RemoveItemCacheAsync(RemoveItemFromUsersCommand model);
}
