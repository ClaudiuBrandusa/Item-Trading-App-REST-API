using Application.Constants;
using Application.Services.Cache;
using Domain.Items;
using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Application.Extensions;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class ItemRepository : RepositoryBase, IItemRepository
{
    public ItemRepository(IDatabaseContextWrapper databaseContextWrapper, ICacheService cacheService) : base(databaseContextWrapper, cacheService)
    {
    }

    public Task<Item[]> ListItemsAsync()
    {
        return context.Items.AsNoTracking().ToArrayAsync();
    }

    public Task<Item[]> ListItemsCachedAsync()
    {
        return cacheService.GetEntitiesAsync(
            CacheKeys.Item.GetItemsKey(),
            (args) => ListItemsAsync(),
            true,
            (Item item) => item.ItemId
        );
    }

    public Task<Item?> GetItemEntityAsync(string itemId) =>
        GetItemQuery(context, itemId);
    
    public Task<Item?> GetItemEntityCachedAsync(string itemId, bool setCache = true)
    {
        return cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(itemId),
            (args) => GetItemEntityAsync(itemId),
            setCache
        );
    }

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Item?>> GetItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string itemId) =>
            context.Items
                .AsNoTracking()
                .FirstOrDefault(x => x.ItemId == itemId)
        );

    #endregion Queries
}
