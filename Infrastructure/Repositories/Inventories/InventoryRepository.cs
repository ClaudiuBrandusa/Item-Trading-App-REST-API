using Domain.Aggregates.Inventories;
using Domain.Repositories.Inventories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Inventories;

public class InventoryRepository : RepositoryBase, IInventoryRepository
{
    public InventoryRepository(DatabaseContext context) : base(context)
    {
    }

    public async Task<Inventory?> GetInventoryAsync(string userId)
    {
        var inventoryEntity = await GetInventoryQuery(_context, userId);

        return inventoryEntity;
    }

    public async Task<Inventory> LoadInventoryAsync(string userId)
    {
        var inventoryEntity = await GetInventoryTrackingQuery(_context, userId);

        if (inventoryEntity is null)
        {
            inventoryEntity = new Inventory(userId);
        }

        return inventoryEntity;
    }

    public async Task<bool> AddInventoryOrUpdateAsync(Inventory inventory)
    {
        var operationResult = false;

        if (await DoesUserInventoryExist(inventory.UserId))
        {
            if (_context.Entry(inventory).State == EntityState.Detached)
            {
                Attach(inventory);
                _context.Entry(inventory).State = EntityState.Modified;
            }

            operationResult = await _context.SaveChangesAsync() > 0;
        }
        else
        {    
            bool result = (await _context.AddAsync(inventory)).State == EntityState.Added;

            operationResult = result && await _context.SaveChangesAsync() > 0;
        }

        Detach(inventory);

        return operationResult;
    }

    public async Task<bool> DropItemAsync(Inventory inventory)
    {
        var operationResult = false;

        if (inventory.ItemIds.Count() == 0)
        {
            operationResult = await RemoveEntityAsync(inventory);
        }
        else
        {
            operationResult = await _context.SaveChangesAsync() > 0;
        }

        Detach(inventory);

        return operationResult;
    }

    public async Task<bool> UpdateInventory(Inventory inventory)
    {
        _context.Update(inventory);

        var operationResult = await _context.SaveChangesAsync() > 0;

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
        _context.Inventories
            .AsNoTracking()
            .Where(x => x.OwnedItems.Any(x => x.ItemId == itemId))
            .Select(x => x.UserId)
            .ToArrayAsync();

    private async Task<bool> DoesUserInventoryExist(string userId)
    {
        return await UserInventoryExistQuery(_context, userId);
    }

    /// <summary>
    /// Returns the free item quantity for the item with <paramref name="itemId"/> owned by the user with <paramref name="userId"/>
    /// </summary>
    private async Task<int> GetItemFreeQuantity(string userId, string itemId)
    {
        var inventoryItemFreeQuantity = await GetInventoryItemFreeQuantityQuery(_context, userId, itemId);

        return inventoryItemFreeQuantity;
    }

    /// <summary>
    /// Returns the item's locked quantity for the item with <paramref name="itemId"/> owned by the user with <paramref name="userId"/>
    /// </summary>
    private async Task<int> GetLockedItemQuantity(string userId, string itemId)
    {
        var lockedItemQuantity = await GetLockedItemQuantityQuery(_context, userId, itemId);

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

    private void Attach(Inventory inventory)
    {
        _context.Attach(inventory);
    }

    private void Detach(Inventory inventory)
    {
        DetachEntity(inventory);
    }
}
