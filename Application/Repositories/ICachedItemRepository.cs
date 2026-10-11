using Domain.Entities.Items;

namespace Application.Repositories;

public interface ICachedItemRepository : ICachedRepository
{
    /// <summary>
    /// Will cache the result if it wasn't cached already.
    /// </summary>
    /// <returns>Item with the given id</returns>
    Task<Item?> GetItemEntityAsync(string itemId, bool setCache = true);

    /// <summary>
    /// Returns the items name in a dictionary where the key is the item id
    /// </summary>
    /// <returns>Items name in a dictionary where the key is the item id</returns>
    Task<Dictionary<string, string>> GetItemsNameAsync(string[] itemIds);

    /// <summary>
    /// Will cache the result if it wasnt cached already.
    /// </summary>
    /// <returns>Items array</returns>
    Task<Item[]> ListItemsAsync(string[] itemIds);
}
