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

    public Task<Item[]> ListItemsAsync()
    {
        return _cacheService.GetEntitiesAsync(
            CacheKeys.Item.GetItemsKey(),
            (args) => _repository.ListItemsAsync(),
            convertEntityToCachedEntity: (Item entity) => _mapper.AdaptToType<Item, CachedItem>(entity),
            convertCachedEntityToEntity: (CachedItem cachedEntity) => _mapper.AdaptToType<CachedItem, Item>(cachedEntity),
            true,
            (Item item) => item.ItemId
        );
    }

    protected override string GetCacheKey(object entity)
    {
        if (entity is not Item item) return string.Empty;

        return CacheKeys.Item.GetItemKey(item.ItemId);
    }

    public void Dispose() => _repository.Dispose();
}
