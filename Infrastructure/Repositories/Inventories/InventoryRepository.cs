using Domain.Aggregates.Inventories;
using Domain.Entities.Inventories;
using Domain.Repositories.Inventories;
using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Inventories;

public class InventoryRepository : RepositoryBase, IInventoryRepository
{
    public InventoryRepository(IDatabaseContextWrapper databaseContextWrapper) : base(databaseContextWrapper)
    {
    }

    public async Task<Inventory?> GetInventoryAsync(string userId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var inventoryEntity = await GetInventoryQuery(context, userId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return inventoryEntity;
    }

    public async Task<bool> AddInventoryAsync(Inventory inventory)
    {
        var storedInventory = await context.Inventories.FindAsync(inventory.UserId);

        bool hasInventory = storedInventory is not null;

        if (hasInventory)
        {
            var itemsToBeAdded = inventory.GetCollectionUpdates(Inventory.CollectionUpdate.Add);

            for (int i = 0; i < itemsToBeAdded.Count; i++)
            {
                var item = inventory.GetItem(itemsToBeAdded[i]);

                storedInventory!.AddItem(item);
            }

            inventory.ResetCollectionUpdates(Inventory.CollectionUpdate.Add);

            var itemsToBeUpdated = inventory.GetCollectionUpdates(Inventory.CollectionUpdate.Update);

            for (int i = 0; i < itemsToBeUpdated.Count; i++)
            {
                var item = inventory.GetItem(itemsToBeUpdated[i]);

                storedInventory!.AddItem(item);
            }

            inventory.ResetCollectionUpdates(Inventory.CollectionUpdate.Update);

            var itemsToBeRemoved = inventory.GetCollectionUpdates(Inventory.CollectionUpdate.Remove);

            for (int i = 0; i < itemsToBeRemoved.Count; i++)
            {
                var item = inventory.GetItem(itemsToBeRemoved[i]);

                storedInventory!.DropItem(item.ItemId, item.Quantity);
            }

            inventory.ResetCollectionUpdates(Inventory.CollectionUpdate.Remove);

            var result = await context.SaveChangesAsync() > 0;

            context.Entry(storedInventory).State = EntityState.Detached;

            return result;
        }

        return await AddEntityAsync(inventory);
    }

    public async Task<bool> DropItemAsync(string userId, string itemId, int amount)
    {
        int freeItemAmount = await GetAmountOfFreeItemAsync(userId, itemId);

        if (freeItemAmount < amount)
            return false;

        freeItemAmount -= amount;

        var inventory = await GetInventoryAsync(userId);

        inventory.DropItem(itemId, amount);

        return await UpdateEntityAsync(inventory);
    }

    public async Task<InventoryItem?> GetOwnedItemEntityAsync(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var inventoryItemEntity = await GetOwnedItemQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return inventoryItemEntity;
    }

    public async Task<LockedItem?> GetLockedInventoryItemEntityAsync(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var lockedInventoryItemEntity = await GetLockedInventoryItemQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return lockedInventoryItemEntity;
    }

    public async Task<int> GetAmountOfLockedItemAsync(string userId, string itemId)
    {
        return await GetLockedItemQuantity(userId, itemId);
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
        var itemQuantity = await GetItemQuantity(userId, itemId);

        int lockedItemQuantity = await GetAmountOfLockedItemAsync(userId, itemId);

        if (itemQuantity == 0) return 0;

        return itemQuantity - lockedItemQuantity;
    }

    public Task<string[]> ListUsersThatOwnItemAsync(string itemId) =>
        context.Inventories
            .AsNoTracking()
            .Where(x => x.OwnedItems.Any(x => x.ItemId == itemId))
            .Select(x => x.UserId)
            .ToArrayAsync();

    /// <summary>
    /// Returns the item quantity for the item with <paramref name="itemId"/> owned by the user with <paramref name="userId"/>
    /// </summary>
    private async Task<int> GetItemQuantity(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var inventoryItemQuantity = await GetInventoryItemQuantityQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return inventoryItemQuantity;
    }

    /// <summary>
    /// Returns the item's locked quantity for the item with <paramref name="itemId"/> owned by the user with <paramref name="userId"/>
    /// </summary>
    private async Task<int> GetLockedItemQuantity(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var lockedItemQuantity = await GetLockedItemQuantityQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return lockedItemQuantity;
    }

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Inventory?>> GetInventoryQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId) =>
            context.Inventories
                .AsNoTracking()
                .FirstOrDefault(oi => Equals(oi.UserId, userId))
        );

    private static readonly Func<DatabaseContext, string, Task<Inventory?>> GetInventoryAsNoTrackingQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId) =>
            context.Inventories
                .AsNoTracking()
                .FirstOrDefault(oi => Equals(oi.UserId, userId))
        );

    private static readonly Func<DatabaseContext, string, string, Task<int>> GetInventoryItemQuantityQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.Inventories
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .SelectMany(oi => oi.OwnedItems)
                .Where(oi => oi.ItemId == itemId)
                .Select(oi => oi.Quantity)
                .FirstOrDefault()
        );

    private static readonly Func<DatabaseContext, string, string, Task<InventoryItem?>> GetOwnedItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.Inventories
                .AsNoTracking()
                .Where(i => Equals(i.UserId, userId))
                .SelectMany(i => i.OwnedItems)
                .FirstOrDefault(oi => Equals(oi.ItemId, itemId))
        );

    private static readonly Func<DatabaseContext, string, string, Task<int>> GetLockedItemQuantityQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.LockedItems
                .AsNoTracking()
                .Where(li => Equals(li.UserId, userId) && Equals(li.ItemId, itemId))
                .Select(x => x.Quantity)
                .FirstOrDefault()
        );

    private static readonly Func<DatabaseContext, string, string, Task<LockedItem?>> GetLockedInventoryItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.LockedItems
                .AsNoTracking()
                .FirstOrDefault(li => Equals(li.UserId, userId) && Equals(li.ItemId, itemId))
        );

    #endregion Queries
}
