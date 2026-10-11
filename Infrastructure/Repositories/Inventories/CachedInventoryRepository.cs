using Application.Constants;
using Application.Services.Cache;
using Domain.Repositories.Inventories;
using Application.Repositories;
using Application.Extensions;
using MapsterMapper;
using Domain.Entities.Inventories;
using Application.Models.Inventories;
using Domain.Aggregates.Inventories;

namespace Infrastructure.Repositories.Inventories;

public class CachedInventoryRepository : CachedRepository, ICachedInventoryRepository
{
    private readonly IInventoryRepository _repository;
    private readonly IMapper _mapper;

    public CachedInventoryRepository(IInventoryRepository repository, ICacheService cacheService, IMapper mapper) : base(repository, cacheService)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Inventory> GetInventoryAsync(string userId)
    {
        var entities = await _cacheService.GetEntitiesAsync(
            CacheKeys.Inventory.GetUserInventoryKey(userId),
            async (args) =>
            {
                var inventory = await _repository.GetInventoryAsync(userId);

                return inventory?.OwnedItems.ToArray() ?? Array.Empty<InventoryItem>();
            },
            true,
            (InventoryItem item) => item.ItemId);

        var inventory = new Inventory(userId);

        for (int i = 0; i < entities?.Length; i++)
        {
            var entity = entities[i];

            inventory.AddItem(entity.ItemId, entity.Quantity);
        }

        return inventory;
    }

    public async Task<Inventory> LoadInventoryAsync(string userId)
    {
        return await _repository.LoadInventoryAsync(userId);
    }

    public async Task<bool> AddInventoryAsync(Inventory inventory)
    {
        var result = await _repository.AddInventoryOrUpdateAsync(inventory);

        if (!result)
            return false;

        if (result)
        {
            var userId = inventory.UserId;
            var inventoryItems = inventory.OwnedItems.ToArray();

            foreach (var inventoryItem in inventoryItems)
            {
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetInventoryItemKey(userId, inventoryItem.ItemId), inventoryItem);
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetItemAmountKey(userId, inventoryItem.ItemId), inventoryItem.FreeAmount);
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, inventoryItem.ItemId), await GetAmountOfLockedItemAsync(userId, inventoryItem.ItemId));
            }
        }

        return result;
    }

    public async Task<bool> DropItemAsync(Inventory inventory, string itemId, int amount)
    {
        var userId = inventory.UserId;

        int freeItemAmount = await GetAmountOfFreeItemAsync(userId, itemId);

        if (freeItemAmount < amount)
            return false;

        bool operationResult = await _repository.DropItemAsync(inventory);

        if (!operationResult) return false;

        freeItemAmount -= amount;

        int lockedAmount = await _repository.GetAmountOfLockedItemAsync(userId, itemId);

        if (freeItemAmount == 0)
        {
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetInventoryItemKey(userId, itemId));
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetItemAmountKey(userId, itemId));
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId));
        }
        else
        {
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetInventoryItemKey(userId, itemId), new InventoryItem(userId, itemId, freeItemAmount + lockedAmount, lockedAmount));
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetItemAmountKey(userId, itemId), freeItemAmount);
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), lockedAmount);
        }

        return true;
    }

    public async Task<bool> DropItemAsync(Inventory inventory, (string itemId, int quantity)[] items)
    {
        var userId = inventory.UserId;

        var cacheChanges = new Dictionary<string, (int lastAmount, int lastLockedAmount)>();

        foreach (var item in items)
        {
            int freeItemAmount = await GetAmountOfFreeItemAsync(userId, item.itemId);

            if (freeItemAmount < item.quantity)
                return false;

            freeItemAmount -= item.quantity;

            int lockedAmount = await _repository.GetAmountOfLockedItemAsync(userId, item.itemId);

            if (freeItemAmount == 0)
            {
                await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetInventoryItemKey(userId, item.itemId));
                await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetItemAmountKey(userId, item.itemId));
                await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, item.itemId));
            }
            else
            {
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetInventoryItemKey(userId, item.itemId), new InventoryItem(userId, item.itemId, freeItemAmount + lockedAmount, lockedAmount));
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetItemAmountKey(userId, item.itemId), freeItemAmount);
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, item.itemId), lockedAmount);
            }

            cacheChanges.Add(item.itemId, (freeItemAmount + item.quantity + lockedAmount, lockedAmount));
        }

        bool operationResult = await _repository.DropItemAsync(inventory);

        if (!operationResult)
        {
            // rollback the cache changes
            foreach (var kvp in cacheChanges)
            {
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetInventoryItemKey(userId, kvp.Key), new InventoryItem(userId, kvp.Key, kvp.Value.lastAmount, kvp.Value.lastLockedAmount));
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetItemAmountKey(userId, kvp.Key), kvp.Value.lastAmount - kvp.Value.lastLockedAmount);
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, kvp.Key), kvp.Value.lastLockedAmount);
            }

            return false;
        }

        return true;
    }

    public async Task<bool> LockItemAsync(Inventory inventory, string itemId, int quantity)
    {
        var userId = inventory.UserId;
        
        var modified = await _repository.UpdateInventory(inventory);

        await AddLockedAmount(userId, itemId, quantity);

        return modified;
    }

    public async Task<bool> LockItemsAsync(Inventory inventory, (string itemId, int quantity)[] items)
    {
        var userId = inventory.UserId;

        var modified = await _repository.AddInventoryOrUpdateAsync(inventory);

        foreach ((var itemId, var quantity) in items)
        {
            await AddLockedAmount(userId, itemId, quantity);
        }

        return modified;
    }

    public async Task<bool> UnlockItemAsync(Inventory inventory, string itemId, int quantity)
    {
        var userId = inventory.UserId;
        var lockedAmount = await GetAmountOfLockedItemAsync(userId, itemId);

        int remainedLockedAmount = lockedAmount - quantity;

        if (remainedLockedAmount < 0)
        {
            return false;
        }

        var operationResult = await _repository.UpdateInventory(inventory);

        if (!operationResult) return false;

        if (remainedLockedAmount == 0)
        {
            // no amount of item remained locked
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId));
        }
        else
        {
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), remainedLockedAmount);
        }

        return true;
    }

    public async Task<bool> UnlockItemsAsync(Inventory inventory, (string itemId, int quantity)[] items)
    {
        var userId = inventory.UserId;
        var cacheChanges = new Dictionary<string, int>();

        foreach (var item in items)
        {    
            var lockedAmount = await GetAmountOfLockedItemAsync(userId, item.itemId);

            int remainedLockedAmount = lockedAmount - item.quantity;

            if (remainedLockedAmount < 0)
            {
                return false;
            }

            if (remainedLockedAmount == 0)
            {
                // no amount of item remained locked
                await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, item.itemId));
            }
            else
            {
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, item.itemId), remainedLockedAmount);
            }

            cacheChanges.Add(item.itemId, lockedAmount + item.quantity);
        }

        var operationResult = await _repository.UpdateInventory(inventory);

        if (!operationResult)
        {
            foreach (var kvp in cacheChanges)
            {
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, kvp.Key), kvp.Value);
            }

            return false;
        }

        return true;
    }

    public Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
    {
        return _cacheService.GetEntityValueAsync(
            CacheKeys.Inventory.GetItemAmountKey(userId, itemId),
            async (args) =>
            {
                var storedFreeItemAmount = await _repository.GetAmountOfFreeItemAsync(userId, itemId);

                return storedFreeItemAmount;
            },
            true);
    }

    public Task<int> GetAmountOfLockedItemAsync(string userId, string itemId)
    {
        return _cacheService.GetEntityValueAsync(
            CacheKeys.Inventory.GetLockedAmountKey(userId, itemId),
            async (args) =>
            {
                var lockedItemAmount = await _repository.GetAmountOfLockedItemAsync(userId, itemId);

                return lockedItemAmount;
            },
            true);
    }

    public Task<string[]> ListUsersThatOwnItemAsync(string itemId) => _repository.ListUsersThatOwnItemAsync(itemId);

    public Task RemoveItemCacheForUserAsync(string userId, string itemId) =>
        Task.WhenAll(
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetInventoryItemKey(userId, itemId)),
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetItemAmountKey(userId, itemId)),
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId))
            );

    private async Task AddLockedAmount(string userId, string itemId, int quantity)
    {
        var cacheKey = CacheKeys.Inventory.GetLockedAmountKey(userId, itemId);

        var currentValue = await _cacheService.GetEntityValueAsync(
            CacheKeys.Inventory.GetLockedAmountKey(userId, itemId),
            async (args) =>
            {
                var lockedItemAmount = await _repository.GetAmountOfLockedItemAsync(userId, itemId);

                return lockedItemAmount;
            },
            true);

        await _cacheService.SetCacheValueAsync(cacheKey, currentValue + quantity);
    }

    protected override string GetCacheKey(object entity)
    {
        if (entity is (InventoryItem ownedItem, string userId))
            return CacheKeys.Inventory.GetInventoryItemKey(userId, ownedItem.ItemId);

        return string.Empty;
    }

    protected override object ConvertBeforeCaching(object entity)
    {
        if (entity is (InventoryItem ownedItem, string userId))
            return _mapper.AdaptToType<InventoryItem, CachedOwnedItem>((InventoryItem)entity, (nameof(CachedOwnedItem.UserId), userId));

        return entity;
    }
}
