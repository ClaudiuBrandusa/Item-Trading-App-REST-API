using Application.Constants;
using Application.Services.Cache;
using Domain.Repositories.Inventory;
using Application.Extensions;
using MapsterMapper;
using Domain.Aggregates.Inventory;
using Domain.Entities.Inventory;
using Application.Models.Inventory;

namespace Infrastructure.Repositories.Inventory;

public class CachedInventoryRepository : CachedRepository, ICachedInventoryRepository
{
    private readonly IInventoryRepository _repository;
    private readonly IMapper _mapper;

    public CachedInventoryRepository(IInventoryRepository repository, ICacheService cacheService, IMapper mapper) : base(repository, cacheService)
    {
        _repository = repository;
        _mapper = mapper;
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
        var item = await GetOwnedItemEntityAsync(userId, itemId);

        int lockedItemQuantity = await GetAmountOfLockedItemAsync(userId, itemId);

        if (item is null) return 0;

        return item!.Quantity - lockedItemQuantity;
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

    public Task<OwnedItem?> GetOwnedItemEntityAsync(string userId, string itemId)
    {
        return _cacheService.GetEntityReferenceAsync(
            CacheKeys.Inventory.GetAmountKey(userId, itemId),
            async (args) =>
            {
                return await _repository.GetOwnedItemEntityAsync(userId, itemId);
            },
            convertEntityToCachedEntity: (OwnedItem entity) => _mapper.AdaptToType<OwnedItem, CachedOwnedItem>(entity),
            convertCachedEntityToEntity: (CachedOwnedItem cachedEntity) => _mapper.AdaptToType<CachedOwnedItem, OwnedItem>(cachedEntity),
            true
        );
    }

    public Task<OwnedItem[]> ListOwnedItemsAsync(string userId)
    {
        return _cacheService.GetEntitiesAsync(
            CacheKeys.Inventory.GetUserInventoryKey(userId),
            async (args) => await _repository.ListOwnedItemsAsync(userId),
            convertEntityToCachedEntity: (OwnedItem entity) => _mapper.AdaptToType<OwnedItem, CachedOwnedItem>(entity),
            convertCachedEntityToEntity: (CachedOwnedItem cachedEntity) => _mapper.AdaptToType<CachedOwnedItem, OwnedItem>(cachedEntity),
            true,
            (OwnedItem item) => item.ItemId);
    }

    public Task<string[]> ListUsersThatOwnItemAsync(string itemId) => _repository.ListUsersThatOwnItemAsync(itemId);

    public Task RemoveItemCacheForUserAsync(string userId, string itemId) =>
        Task.WhenAll(
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetAmountKey(userId, itemId)),
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId))
            );

    protected override string GetCacheKey(object entity)
    {
        if (entity is not OwnedItem ownedItem) return string.Empty;

        return CacheKeys.Inventory.GetAmountKey(ownedItem.UserId, ownedItem.ItemId);
    }

    protected override object ConvertBeforeCaching(object entity)
    {
        return _mapper.AdaptToType<OwnedItem, CachedOwnedItem>((OwnedItem)entity);
    }

    public void Dispose() => _repository.Dispose();
}
