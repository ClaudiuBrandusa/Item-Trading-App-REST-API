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

        inventory.ResetCollectionUpdates(Inventory.CollectionUpdate.Add);

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
                await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetAmountKey(userId, inventoryItem.ItemId), inventoryItem);
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

        bool operationResult = await _repository.DropItemAsync(inventory, itemId, amount);

        if (!operationResult) return false;

        freeItemAmount -= amount;

        int lockedAmount = await _repository.GetAmountOfLockedItemAsync(userId, itemId);

        if (freeItemAmount == 0)
        {
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetAmountKey(userId, itemId));
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId));
        }
        else
        {
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetAmountKey(userId, itemId), new InventoryItem(userId, itemId, freeItemAmount + lockedAmount, lockedAmount));
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), lockedAmount);
        }

        return true;
    }

    public async Task<bool> LockItemAsync(Inventory inventory, string itemId, int quantity)
    {
        var userId = inventory.UserId;
        
        var modified = await _repository.UpdateInventory(inventory);

        await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), quantity);

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

    public async Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
    {
        var inventoryItem = await _cacheService.GetCacheValueAsync<InventoryItem?>(CacheKeys.Inventory.GetAmountKey(userId, itemId));
        
        if (inventoryItem is null)
        {
            return await _repository.GetAmountOfFreeItemAsync(userId, itemId);
        }

        return inventoryItem?.FreeAmount ?? 0;
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
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetAmountKey(userId, itemId)),
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId))
            );

    private async Task<bool> UpdateInventoryItemCache(InventoryItem ownedItem, string userId)
    {
        await SetCacheAsync((ownedItem, userId));

        return true;
    }

    protected override string GetCacheKey(object entity)
    {
        if (entity is (InventoryItem ownedItem, string userId))
            return CacheKeys.Inventory.GetAmountKey(userId, ownedItem.ItemId);
        else if (entity is LockedItem lockedItem)
            return CacheKeys.Inventory.GetLockedAmountKey(lockedItem.UserId, lockedItem.ItemId);

        return string.Empty;
    }

    protected override object ConvertBeforeCaching(object entity)
    {
        if (entity is (InventoryItem ownedItem, string userId))
            return _mapper.AdaptToType<InventoryItem, CachedOwnedItem>((InventoryItem)entity, (nameof(CachedOwnedItem.UserId), userId));
        else if (entity is LockedItem lockedItem)
            return lockedItem.Quantity;

        return entity;
    }

    public void Dispose() => _repository.Dispose();
}
