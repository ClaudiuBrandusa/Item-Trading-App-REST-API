using Domain.Entities.Items;

namespace Domain.Repositories.Items;

public interface IItemRepository : IRepository, IDisposable
{
    /// <returns>Items array</returns>
    Task<Item[]> ListItemsAsync();

    /// <returns>Item with the given id</returns>
    Task<Item?> GetItemEntityAsync(string itemId);
}
