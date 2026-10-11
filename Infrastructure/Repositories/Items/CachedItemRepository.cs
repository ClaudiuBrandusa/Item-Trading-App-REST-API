using Application.Constants;
using Application.Services.Cache;
using Domain.Entities.Items;
using Domain.Repositories.Items;
using Application.Extensions;
using Application.Models.Items;
using MapsterMapper;
using Application.Repositories;

namespace Infrastructure.Repositories.Items;

public class CachedItemRepository : CachedRepository, ICachedItemRepository
{
    private readonly IItemRepository _repository;
    private readonly IMapper _mapper;

    public CachedItemRepository(IItemRepository repository, ICacheService cacheService, IMapper mapper) : base(repository, cacheService)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public Task<Item?> GetItemEntityAsync(string itemId, bool setCache = true)
    {
        return _cacheService.GetEntityReferenceAsync(
            CacheKeys.Item.GetItemKey(itemId),
            (args) => _repository.GetItemEntityAsync(itemId),
            convertEntityToCachedEntity: (Item entity) => _mapper.AdaptToType<Item, CachedItem>(entity),
            convertCachedEntityToEntity: (CachedItem cachedEntity) => _mapper.AdaptToType<CachedItem, Item>(cachedEntity),
            setCache
        );
    }

    public async Task<Dictionary<string, string>> GetItemsNameAsync(string[] itemIds)
    {
        var cacheKeys = itemIds.Select(CacheKeys.Item.GetItemNameKey).ToArray();

        var cachedResult = await _cacheService.GetCacheValuesAsync(cacheKeys);
    
        if (cachedResult.Count > 0)
        {
            // replace the cache key with the item id
            var resultDictionary = cachedResult.ToDictionary(
                kvp => CacheKeys.Item.ExtractItemIdFromItemKey(kvp.Key),
                kvp => kvp.Value
            );

            if (cachedResult.Count < cacheKeys.Length)
            {
                // some cache misses
                var remainedItemIds = itemIds.Except(cachedResult.Keys).ToArray();
            
                var itemNamesMissedFromCache = await _repository.GetItemsNameAsync(remainedItemIds);
            
                var newCacheKeys = new string[itemNamesMissedFromCache.Count];

                foreach (var kvp in itemNamesMissedFromCache)
                {
                    if (!resultDictionary.TryAdd(kvp.Key, kvp.Value))
                    {
                        continue;
                    }
                }

                // write to cache the missed names
                await _cacheService.SetCacheValuesAsync(
                    itemNamesMissedFromCache.ToDictionary(
                        kvp => CacheKeys.Item.GetItemNameKey(kvp.Key),
                        kvp => kvp.Value
                    )
                );
            }

            return resultDictionary;
        }
        else
        {
            var resultDictionary = await _repository.GetItemsNameAsync(itemIds);
        
            return resultDictionary;
        }
    }

    public Task<Item[]> ListItemsAsync(string[] itemIds)
    {
        return _cacheService.GetEntitiesAsync(
            CacheKeys.Item.GetItemsKey(),
            itemIds,
            (args) => _repository.ListItemsAsync(itemIds),
            convertEntityToCachedEntity: (Item entity) => _mapper.AdaptToType<Item, CachedItem>(entity),
            convertCachedEntityToEntity: (CachedItem cachedEntity) => _mapper.AdaptToType<CachedItem, Item>(cachedEntity),
            true,
            (Item item) => item.ItemId
        );
    }

    protected override async Task SetCacheAsync(object entity)
    {
        if (entity is Item item)
        {
            await _cacheService.SetCacheValueAsync(CacheKeys.Item.GetItemNameKey(item.ItemId), item.Name);
        }

        await base.SetCacheAsync(entity);
    }

    protected override string GetCacheKey(object entity)
    {
        if (entity is not Item item) return string.Empty;

        return CacheKeys.Item.GetItemKey(item.ItemId);
    }
}
