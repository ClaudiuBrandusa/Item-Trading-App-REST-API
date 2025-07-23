using Domain.Aggregates.Inventories;
using Domain.Entities.Inventories;

namespace Domain.Repositories.Inventories;

public interface IInventoryRepository : IRepository, IDisposable
{
    /// <summary>
    /// Drops the <paramref name="amount"/> of item with <paramref name="itemId"/> from the user's inventory
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <param name="amount"></param>
    /// <returns>Operation result</returns>
    Task<bool> DropItemAsync(string userId, string itemId, int amount);

    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>The amount of an item with <paramref name="itemId"/> that is locked in the inventory of the user with <paramref name="userId"/></returns>
    Task<int> GetAmountOfLockedItemAsync(string userId, string itemId);

    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>Inventory item with <paramref name="itemId"/> of the user with the given <paramref name="userId"/></returns>
    Task<OwnedItem?> GetOwnedItemEntityAsync(string userId, string itemId);

    /// <param name="userId"></param>
    /// <returns>List with the items from the inventory of the user with the given <paramref name="userId"/></returns>
    Task<OwnedItem[]> ListOwnedItemsAsync(string userId);

    /// <param name="itemId"></param>
    /// <returns>List of users that own the item with <paramref name="itemId"/></returns>
    Task<string[]> ListUsersThatOwnItemAsync(string itemId);

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

    /// <summary>
    /// Unlocks the amount <paramref name="quantity"/> of item with <paramref name="itemId"/> in the inventory of the user with <paramref name="userId"/>
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <param name="quantity"></param>
    /// <returns>Operation result</returns>
    Task<bool> UnlockItemAsync(string userId, string itemId, int quantity);

    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>The amount of locked item with <paramref name="itemId"/> from the inventory of the user with <paramref name="userId"/></returns>
    Task<LockedItem?> GetLockedInventoryItemEntityAsync(string userId, string itemId);
}
