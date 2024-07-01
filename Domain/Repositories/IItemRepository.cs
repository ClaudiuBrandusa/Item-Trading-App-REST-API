namespace Domain.Repositories;

public interface IItemRepository : IRepository, IDisposable
{
    /// <returns>Items array</returns>
    Task<Items.Item[]> ListItemsAsync();

    /// <summary>
    /// Will cache the result if it wasnt cached already.
    /// </summary>
    /// <returns>Items array</returns>
    Task<Items.Item[]> ListItemsCachedAsync();

    /// <returns>Item with the given id</returns>
    Task<Items.Item?> GetItemEntityAsync(string itemId);

    /// <summary>
    /// Will cache the result if it wasnt cached already.
    /// </summary>
    /// <returns>Item with the given id</returns>
    Task<Items.Item?> GetItemEntityCachedAsync(string itemId, bool setCache = true);
}
