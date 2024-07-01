using Domain.Inventory;

namespace Domain.Repositories;

public interface IInventoryRepository : IRepository, IDisposable
{
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>Inventory item with <paramref name="itemId"/> of the user with the given <paramref name="userId"/></returns>
    Task<OwnedItem?> GetInventoryItemEntityAsync(string userId, string itemId);

    /// <param name="userId"></param>
    /// <returns>List with the items from the inventory of the user with the given <paramref name="userId"/></returns>
    Task<OwnedItem[]> ListOwnedItemsAsync(string userId);

    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>List with the items from the inventory of the user with the given <paramref name="userId"/></returns>
    Task<InventoryItem[]> ListInventoryItemsCachedAsync(string userId);

    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>The amount of an item with <paramref name="itemId"/> that is locked in the inventory of the user with <paramref name="userId"/></returns>
    Task<int> GetAmountOfLockedItemCachedAsync(string userId, string itemId);

    /// <param name="itemId"></param>
    /// <returns>List of users that own the item with <paramref name="itemId"/></returns>
    Task<string[]> ListUsersThatOwnItemAsync(string itemId);

    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>Inventory item with <paramref name="itemId"/> of the user with the given <paramref name="userId"/></returns>
    Task<InventoryItem?> GetInventoryItemEntityCachedAsync(string userId, string itemId);

    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>The amount of free item with <paramref name="itemId"/> from the inventory of the user with <paramref name="userId"/></returns>
    Task<int> GetAmountOfFreeItemAsync(string userId, string itemId);

    /// <summary>
    /// Locks the amount <paramref name="quantity"/> of item with <paramref name="itemId"/> in the inventory of the user with <paramref name="userId"/>
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <param name="quantity"></param>
    /// <returns>Operation result</returns>
    Task<bool> LockItemAsync(string userId, string itemId, int quantity);

    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>The amount of locked item with <paramref name="itemId"/> from the inventory of the user with <paramref name="userId"/></returns>
    Task<LockedItem?> GetLockedInventoryItemEntityAsync(string userId, string itemId);

    /// <summary>
    /// Removes the cache of item with <paramref name="itemId"/> from the inventory of user with <paramref name="userId"/>
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    Task RemoveItemCacheForUserAsync(string userId, string itemId);
}
