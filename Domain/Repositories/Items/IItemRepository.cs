using Domain.Entities.Items;

namespace Domain.Repositories.Items;

public interface IItemRepository : IRepository
{
    /// <returns>Items array</returns>
    Task<Item[]> ListItemsAsync(string[] itemIds);

    /// <returns>Items name with their item id</returns>
    Task<Dictionary<string, string>> GetItemsNameAsync(string[] itemIds);

    /// <returns>Item with the given id</returns>
    Task<Item?> GetItemEntityAsync(string itemId);
}
