using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Items;
using Domain.Repositories.Items;

namespace Infrastructure.Repositories.Items;

public class ItemRepository : RepositoryBase, IItemRepository
{
    public ItemRepository(DatabaseContext context) : base(context)
    {
    }

    public Task<Item[]> ListItemsAsync(string[] itemIds)
    {
        if (itemIds.Length == 0)
        {
            return _context.Items
                .AsNoTracking()
                .ToArrayAsync();
        }
        else
        {
            return _context.Items
                .AsNoTracking()
                .Where(item => itemIds.Contains(item.ItemId))
                .ToArrayAsync();
        }
    }

    public async Task<Dictionary<string, string>> GetItemsNameAsync(string[] itemIds)
    {
        return await _context.Items
            .AsNoTracking()
            .Where(item => itemIds.Contains(item.ItemId))
            .Select(item => new { item.ItemId, item.Name })
            .ToDictionaryAsync(
                x => x.ItemId,
                x => x.Name
            );
    }

    public Task<Item?> GetItemEntityAsync(string itemId) =>
        GetItemQuery(_context, itemId);

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Item?>> GetItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string itemId) =>
            context.Items
                .AsNoTracking()
                .FirstOrDefault(x => x.ItemId == itemId)
        );

    #endregion Queries
}
