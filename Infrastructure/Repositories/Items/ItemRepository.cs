using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Items;
using Domain.Repositories.Items;

namespace Infrastructure.Repositories.Items;

public class ItemRepository : RepositoryBase, IItemRepository
{
    public ItemRepository(IDatabaseContextWrapper databaseContextWrapper) : base(databaseContextWrapper)
    {
    }

    public Task<Item[]> ListItemsAsync()
    {
        return context.Items
            .AsNoTracking()
            .ToArrayAsync();
    }

    public Task<Item?> GetItemEntityAsync(string itemId) =>
        GetItemQuery(context, itemId);

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Item?>> GetItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string itemId) =>
            context.Items
                .AsNoTracking()
                .FirstOrDefault(x => x.ItemId == itemId)
        );

    #endregion Queries
}
