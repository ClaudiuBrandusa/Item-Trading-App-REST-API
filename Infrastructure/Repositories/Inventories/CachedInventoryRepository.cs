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

    public async Task<Inventory?> GetInventoryAsync(string userId)
    {
        var entities = await _cacheService.GetEntitiesAsync(
            CacheKeys.Inventory.GetUserInventoryKey(userId),
            async (args) => (await _repository.GetInventoryAsync(userId)).OwnedItems.ToArray(),
            convertEntityToCachedEntity: (InventoryItem entity) => _mapper.AdaptToType<InventoryItem, CachedOwnedItem>(entity, (nameof(CachedOwnedItem.UserId), userId)),
            convertCachedEntityToEntity: (CachedOwnedItem cachedEntity) => _mapper.AdaptToType<CachedOwnedItem, InventoryItem>(cachedEntity),
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

    public async Task<bool> AddInventoryAsync(Inventory inventory)
    {
        return await _repository.AddInventoryAsync(inventory);
    }

    public async Task<bool> DropItemAsync(string userId, string itemId, int amount)
    {
        int freeItemAmount = await GetAmountOfFreeItemAsync(userId, itemId);

        if (freeItemAmount < amount)
            return false;

        bool operationResult = await _repository.DropItemAsync(userId, itemId, amount);

        if (!operationResult) return false;

        freeItemAmount -= amount;

        if (freeItemAmount == 0)
        {
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetAmountKey(userId, itemId));
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId));
        }
        else
        {
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetAmountKey(userId, itemId), new CachedOwnedItem { ItemId = itemId, UserId = userId, Quantity = freeItemAmount });
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), await GetAmountOfLockedItemAsync(userId, itemId));
        }

        return true;
    }

    public async Task<bool> LockItemAsync(string userId, string itemId, int quantity)
    {
        bool storedInDb = await _repository.GetLockedInventoryItemEntityAsync(userId, itemId) is not null;
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

        await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), quantity);

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

        var operationResult = await _repository.UnlockItemAsync(userId, itemId, quantity);

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
        // get amount of free item from cache
        int? itemQuantity = await _cacheService.GetCacheValueAsync<int?>(CacheKeys.Inventory.GetAmountKey(userId, itemId));

        // if miss
        if (itemQuantity is null)
        {
            // then get from repository
            itemQuantity = await _repository.GetAmountOfFreeItemAsync(userId, itemId);
        }

        int lockedItemQuantity = await GetAmountOfLockedItemAsync(userId, itemId);

        return itemQuantity.Value - lockedItemQuantity;
    }

    public Task<int> GetAmountOfLockedItemAsync(string userId, string itemId)
    {
        return _cacheService.GetEntityValueAsync(
            CacheKeys.Inventory.GetLockedAmountKey(userId, itemId),
            async (args) =>
            {
                var entity = await _repository.GetLockedInventoryItemEntityAsync(userId, itemId);

                return entity?.Quantity ?? 0;
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
