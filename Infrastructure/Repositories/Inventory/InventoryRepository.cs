using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Inventory;
using Domain.Aggregates.Inventory;
using Domain.Repositories.Inventory;

namespace Infrastructure.Repositories.Inventory;

public class InventoryRepository : RepositoryBase, IInventoryRepository
{
    private readonly IMapper _mapper;

    public InventoryRepository(IDatabaseContextWrapper databaseContextWrapper, IMapper mapper) : base(databaseContextWrapper)
    {
        _mapper = mapper;
    }

    public async Task<bool> DropItemAsync(string userId, string itemId, int amount)
    {
        int freeItemAmount = await GetAmountOfFreeItemAsync(userId, itemId);

        if (freeItemAmount < amount)
            return false;

        freeItemAmount -= amount;

        var entity = new OwnedItem(itemId, userId, freeItemAmount);

        if (freeItemAmount == 0)
        {
            return await RemoveEntityAsync(entity);
        }
        else
        {
            return await UpdateEntityAsync(entity);
        }
    }

    public async Task<OwnedItem?> GetOwnedItemEntityAsync(string userId, string itemId)
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

    public async Task<LockedItem?> GetLockedInventoryItemEntityAsync(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var lockedInventoryItemEntity = await GetLockedInventoryItemQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.Dispose(dbContext);

        return lockedInventoryItemEntity;
    }

    public async Task<int> GetAmountOfLockedItemAsync(string userId, string itemId)
    {
        var entity = await GetLockedInventoryItemEntityAsync(userId, itemId);

        return entity?.Quantity ?? 0;
    }

    public async Task<bool> LockItemAsync(string userId, string itemId, int quantity)
    {
        bool storedInDb = await GetLockedInventoryItemEntityAsync(userId, itemId) is not null;
        bool modified;

        if (!storedInDb)
        {
            modified = await AddEntityAsync(new LockedItem(userId, itemId, quantity));
        }
        else
        {
            int lockedAmount = await GetAmountOfLockedItemAsync(userId, itemId);

            quantity += lockedAmount;

            modified = await UpdateEntityAsync(new LockedItem(userId, itemId, quantity));
        }

        return modified;
    }

    public async Task<bool> UnlockItemAsync(string userId, string itemId, int quantity)
    {
        var lockedAmount = await GetAmountOfLockedItemAsync(userId, itemId);

        int remainedLockedAmount = lockedAmount - quantity;

        if (remainedLockedAmount < 0)
        {
            return false;
        }

        var entity = new LockedItem(userId, itemId, remainedLockedAmount);

        if (remainedLockedAmount == 0)
        {
            return await RemoveEntityAsync(entity);
        }
        else
        {
            return await UpdateEntityAsync(entity);
        }
    }

    public async Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
    {
        var item = await GetOwnedItemEntityAsync(userId, itemId);

        int lockedItemQuantity = await GetAmountOfLockedItemAsync(userId, itemId);

        if (item is null) return 0;

        return item!.Quantity - lockedItemQuantity;
    }

    public Task<string[]> ListUsersThatOwnItemAsync(string itemId) =>
        context.OwnedItems
            .AsNoTracking()
            .Where(x => x.ItemId == itemId).Select(x => x.UserId)
            .ToArrayAsync();

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
                .FirstOrDefault(li => Equals(li.UserId, userId) && Equals(li.ItemId, itemId))
        );

    #endregion Queries
}
