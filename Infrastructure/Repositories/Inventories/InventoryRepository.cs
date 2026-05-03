using Domain.Aggregates.Inventories;
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

        var inventoryEntity = await GetInventoryQuery(dbContext, userId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return inventoryEntity;
    }

    public async Task<Inventory> LoadInventoryAsync(string userId)
    {
        var inventoryEntity = await GetInventoryTrackingQuery(context, userId);

        if (inventoryEntity is null)
        {
            inventoryEntity = new Inventory(userId);
        }

        return inventoryEntity;
    }

    public void Attach(Inventory inventory)
    {
        context.Attach(inventory);
    }

    public void Detach(Inventory inventory)
    {
        context.Entry(inventory).State = EntityState.Detached;
    }

    public async Task<bool> AddInventoryOrUpdateAsync(Inventory inventory)
    {
        var operationResult = false;

        if (await DoesUserInventoryExist(inventory.UserId))
        {
            operationResult = await context.SaveChangesAsync() > 0;
        }
        else
        {    
            bool result = (await context.AddAsync(inventory)).State == EntityState.Added;

            operationResult = result && await context.SaveChangesAsync() > 0;
        }

        Detach(inventory);

        return operationResult;
    }

    public async Task<bool> DropItemAsync(Inventory inventory, string itemId, int amount)
    {
        var operationResult = false;

        if (inventory.ItemIds.Count() == 0)
        {
            operationResult = await RemoveEntityAsync(inventory);
        }
        else
        {
            operationResult = await context.SaveChangesAsync() > 0;
        }

        Detach(inventory);

        return operationResult;
    }

    public async Task<bool> UpdateInventory(Inventory inventory)
    {
        context.Update(inventory);

        var operationResult = await context.SaveChangesAsync() > 0;

        Detach(inventory);

        return operationResult;
    }

    public async Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
    {
        return await GetItemFreeQuantity(userId, itemId);
    }

    public async Task<int> GetAmountOfLockedItemAsync(string userId, string itemId)
    {
        return await GetLockedItemQuantity(userId, itemId);
    }

    public Task<string[]> ListUsersThatOwnItemAsync(string itemId) =>
        context.Inventories
            .AsNoTracking()
            .Where(x => x.OwnedItems.Any(x => x.ItemId == itemId))
            .Select(x => x.UserId)
            .ToArrayAsync();

    private async Task<bool> DoesUserInventoryExist(string userId)
    {
        return await UserInventoryExistQuery(context, userId);
    }

    /// <summary>
    /// Returns the free item quantity for the item with <paramref name="itemId"/> owned by the user with <paramref name="userId"/>
    /// </summary>
    private async Task<int> GetItemFreeQuantity(string userId, string itemId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var inventoryItemFreeQuantity = await GetInventoryItemFreeQuantityQuery(dbContext, userId, itemId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return inventoryItemFreeQuantity;
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
                .Include(x => x.OwnedItems)
                .AsNoTracking()
                .FirstOrDefault(oi => Equals(oi.UserId, userId))
        );

    private static readonly Func<DatabaseContext, string, Task<bool>> UserInventoryExistQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId) =>
            context.Inventories
                .AsNoTracking()
                .Where(oi => Equals(oi.UserId, userId))
                .Any()
        );

    private static readonly Func<DatabaseContext, string, Task<Inventory?>> GetInventoryTrackingQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId) =>
            context.Inventories
                .Include(x => x.OwnedItems)
                .FirstOrDefault(oi => Equals(oi.UserId, userId))
        );

    private static readonly Func<DatabaseContext, string, string, Task<int>> GetInventoryItemFreeQuantityQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.Inventories
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .SelectMany(oi => oi.OwnedItems)
                .Where(oi => oi.ItemId == itemId)
                .Select(oi => oi.Quantity - oi.LockedAmount)
                .FirstOrDefault()
        );

    private static readonly Func<DatabaseContext, string, string, Task<int>> GetLockedItemQuantityQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.Inventories
                .Include(x => x.OwnedItems)
                .AsNoTracking()
                .Where(i => Equals(i.UserId, userId))
                .SelectMany(oi => oi.OwnedItems)
                .Where(i => i.ItemId == itemId)
                .Select(i => i.LockedAmount)
                .FirstOrDefault()
        );

    #endregion Queries
}
