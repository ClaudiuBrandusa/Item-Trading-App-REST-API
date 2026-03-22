using Domain.Aggregates.Inventories;

namespace Domain.Repositories.Inventories;

public interface IInventoryRepository : IRepository, IDisposable
{
    /// <summary>
    /// Returns the inventory for the given userId (for reading purposes)
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Inventory?> GetInventoryAsync(string userId);

    /// <summary>
    /// Returns the inventory for the given userId (for writing purposes)
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Inventory> LoadInventoryAsync(string userId);

    /// <summary>
    /// Attaches the given inventory instance to the database context
    /// </summary>
    void Attach(Inventory inventory);

    /// <summary>
    /// Detaches the given inventory instance from the database context
    /// </summary>
    void Detach(Inventory inventory);

    /// <summary>
    /// Add inventory for the user
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    Task<bool> AddInventoryOrUpdateAsync(Inventory inventory);

    /// <summary>
    /// Drops the <paramref name="amount"/> of item with <paramref name="itemId"/> from the user's inventory
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <param name="amount"></param>
    /// <returns>Operation result</returns>
    Task<bool> DropItemAsync(Inventory inventory, string itemId, int amount);

    /// <summary>
    /// Update inventory aggregate
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    Task<bool> UpdateInventory(Inventory inventory);

    /// <param name="itemId"></param>
    /// <returns>List of users that own the item with <paramref name="itemId"/></returns>
    Task<string[]> ListUsersThatOwnItemAsync(string itemId);

    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>The amount of free item with <paramref name="itemId"/> from the inventory of the user with <paramref name="userId"/></returns>
    Task<int> GetAmountOfFreeItemAsync(string userId, string itemId);

    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns>The amount of an item with <paramref name="itemId"/> that is locked in the inventory of the user with <paramref name="userId"/></returns>
    Task<int> GetAmountOfLockedItemAsync(string userId, string itemId);
}
