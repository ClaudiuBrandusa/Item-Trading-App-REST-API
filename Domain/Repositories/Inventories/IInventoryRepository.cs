using Domain.Aggregates.Inventories;

namespace Domain.Repositories.Inventories;

public interface IInventoryRepository : IRepository
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
    /// Add inventory for the user
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    Task<bool> AddInventoryOrUpdateAsync(Inventory inventory);

    /// <summary>
    /// The drop operation/s are already performed before reaching this method,
    /// so this method only handles the decision of updating the drop operation or
    /// deleting the whole inventory (in case no other item remained)
    /// </summary>
    /// <returns>Operation result</returns>
    Task<bool> DropItemAsync(Inventory inventory);

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
