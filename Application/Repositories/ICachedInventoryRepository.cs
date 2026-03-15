using Domain.Aggregates.Inventories;

namespace Application.Repositories;

public interface ICachedInventoryRepository : ICachedRepository, IDisposable
{
    /// <summary>
    /// Retrieves the user's inventory for reading purposes
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>User's inventory</returns>
    Task<Inventory> GetInventoryAsync(string userId);

    /// <summary>
    /// Loads the user's inventory for writing purposes. This instace is tracked by the context.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Inventory> LoadInventoryAsync(string userId);

    /// <summary>
    /// Adds a new inventory instance to the database
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns>Operation result</returns>
    Task<bool> AddInventoryAsync(Inventory inventory);

    /// <summary>
    /// Drops the <paramref name="amount"/> of item with <paramref name="itemId"/> from the user's inventory
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <param name="amount"></param>
    /// <returns>Operation result</returns>
    Task<bool> DropItemAsync(Inventory inventory, string itemId, int amount);

    /// <summary>
    /// Locks the amount <paramref name="quantity"/> of item with <paramref name="itemId"/> in the inventory of the user with <paramref name="userId"/>
    /// </summary>
    /// <returns>Operation result</returns>
    Task<bool> LockItemAsync(Inventory inventory, string itemId, int quantity);

    /// <summary>
    /// Unlocks the amount <paramref name="quantity"/> of item with <paramref name="itemId"/> in the inventory of the user with <paramref name="userId"/>
    /// </summary>
    /// <returns>Operation result</returns>
    Task<bool> UnlockItemAsync(Inventory inventory, string itemId, int quantity);

    /// <returns>The amount of free item with <paramref name="itemId"/> from the inventory of the user with <paramref name="userId"/></returns>
    Task<int> GetAmountOfFreeItemAsync(string userId, string itemId);

    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <returns>The amount of an item with <paramref name="itemId"/> that is locked in the inventory of the user with <paramref name="userId"/></returns>
    Task<int> GetAmountOfLockedItemAsync(string userId, string itemId);

    /// <returns>List of users that own the item with <paramref name="itemId"/></returns>
    Task<string[]> ListUsersThatOwnItemAsync(string itemId);

    /// <summary>
    /// Removes the cache of item with <paramref name="itemId"/> from the inventory of user with <paramref name="userId"/>
    /// </summary>
    Task RemoveItemCacheForUserAsync(string userId, string itemId);
}
