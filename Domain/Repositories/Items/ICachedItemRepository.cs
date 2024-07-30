using Domain.Entities.Items;

namespace Domain.Repositories.Items;

public interface ICachedItemRepository : ICachedRepository, IDisposable
{
    /// <summary>
    /// Will cache the result if it wasnt cached already.
    /// </summary>
    /// <returns>Item with the given id</returns>
    Task<Item?> GetItemEntityAsync(string itemId, bool setCache = true);

    /// <summary>
    /// Will cache the result if it wasnt cached already.
    /// </summary>
    /// <returns>Items array</returns>
    Task<Item[]> ListItemsAsync();
}
