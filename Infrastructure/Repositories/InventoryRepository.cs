using Application.Constants;
using Application.Services.Cache;
using Domain.Inventory;
using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Application.Extensions;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class InventoryRepository : RepositoryBase, IInventoryRepository
{
    private readonly IMapper _mapper;

    public InventoryRepository(IDatabaseContextWrapper databaseContextWrapper, ICacheService cacheService, IMapper mapper) : base(databaseContextWrapper, cacheService)
    {
        _mapper = mapper;
    }

    public async Task<OwnedItem?> GetInventoryItemEntityAsync(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var inventoryItemEntity = await GetInventoryItemQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.Dispose(dbContext);

        return inventoryItemEntity;
    }

    public async Task<OwnedItem[]> ListOwnedItemsAsync(string userId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var array = await dbContext
            .OwnedItems
                .AsNoTracking()
                .Where(oi => Equals(oi.UserId, userId))
                .ToArrayAsync();

        DatabaseContextWrapper.Dispose(dbContext);

        return array;
    }

    public Task<InventoryItem[]> ListInventoryItemsCachedAsync(string userId)
    {
        return cacheService.GetEntitiesAsync(
            CacheKeys.Inventory.GetUserInventoryKey(userId),
            async (args) => (await ListOwnedItemsAsync(userId))
                    .Select(item => _mapper.AdaptToType<OwnedItem, InventoryItem>(item))
                    .ToArray(),
            true,
            (InventoryItem item) => item.Id);
    }

    public async Task<LockedItem?> GetLockedInventoryItemEntityAsync(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var lockedInventoryItemEntity = await GetLockedInventoryItemQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.Dispose(dbContext);

        return lockedInventoryItemEntity;
    }

    public Task<int> GetAmountOfLockedItemCachedAsync(string userId, string itemId)
    {
        return cacheService.GetEntityAsync(
            CacheKeys.Inventory.GetLockedAmountKey(userId, itemId),
            async (args) =>
            {
                var entity = await GetLockedInventoryItemEntityAsync(userId, itemId);

                return entity?.Quantity ?? 0;
            },
            true);
    }

    public Task<InventoryItem?> GetInventoryItemEntityCachedAsync(string userId, string itemId)
    {
        return cacheService.GetEntityAsync(
            CacheKeys.Inventory.GetAmountKey(userId, itemId),
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(userId, itemId);

                if (entity is not null)
                {
                    return _mapper.AdaptToType<OwnedItem, InventoryItem>(entity);
                }

                return null;
            }
        );
    }

    public async Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
    {
        var item = await GetInventoryItemEntityCachedAsync(userId, itemId);

        int lockedItemQuantity = await GetAmountOfLockedItemCachedAsync(userId, itemId);

        if (item is null) return 0;

        return item!.Quantity - lockedItemQuantity;
    }

    public async Task<bool> LockItemAsync(string userId, string itemId, int quantity)
    {
        bool storedInDb = await GetLockedInventoryItemEntityAsync(userId, itemId) != default;
        bool modified;

        if (!storedInDb)
        {
            modified = await AddEntityAsync(new LockedItem { UserId = userId, ItemId = itemId, Quantity = quantity });
        }
        else
        {
            int lockedAmount = await GetAmountOfLockedItemCachedAsync(userId, itemId);

            quantity += lockedAmount;

            modified = await UpdateEntityAsync(new LockedItem { UserId = userId, ItemId = itemId, Quantity = quantity });
        }

        await cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), quantity);

        return modified;
    }

    public Task<string[]> ListUsersThatOwnItemAsync(string itemId) =>
        context.OwnedItems
            .AsNoTracking()
            .Where(x => x.ItemId == itemId).Select(x => x.UserId)
            .ToArrayAsync();

    public Task RemoveItemCacheForUserAsync(string userId, string itemId) =>
        Task.WhenAll(
                cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetAmountKey(userId, itemId)),
                cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId))
            );

    #region Queries

    private static readonly Func<DatabaseContext, string, string, Task<OwnedItem?>> GetInventoryItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.OwnedItems
                .AsNoTracking()
                .FirstOrDefault(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId))
        );

    private static readonly Func<DatabaseContext, string, string, Task<LockedItem?>> GetLockedInventoryItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.LockedItems
                .AsNoTracking()
                .FirstOrDefault(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId))
        );

    #endregion Queries
}
