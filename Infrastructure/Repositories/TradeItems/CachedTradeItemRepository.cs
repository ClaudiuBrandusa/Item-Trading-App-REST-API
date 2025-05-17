using Application.Constants;
using Application.Services.Cache;
using Domain.Entities.Trades;
using Domain.Repositories.TradeItems;
using Application.Extensions;
using MapsterMapper;
using Application.Models.TradeItems;
using Application.Repositories;

namespace Infrastructure.Repositories.TradeItems;

public class CachedTradeItemRepository : CachedRepository, ICachedTradeItemRepository
{
    private readonly ITradeItemRepository _repository;
    private readonly IMapper _mapper;

    public CachedTradeItemRepository(ITradeItemRepository repository, ICacheService cacheService, IMapper mapper) : base(repository, cacheService)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public Task<TradeItem?> GetTradeItemAsync(string tradeId, string itemId)
    {
        return _cacheService.GetEntityReferenceAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, itemId), async (args) =>
        {
            return await _repository.GetTradeItemAsync(tradeId, itemId);
        },
        convertEntityToCachedEntity: (TradeItem entity) => _mapper.AdaptToType<TradeItem, CachedTradeItem>(entity),
        convertCachedEntityToEntity: (CachedTradeItem cachedEntity) => _mapper.AdaptToType<CachedTradeItem, TradeItem>(cachedEntity),
        true);
    }

    public Task<TradeItem[]> ListTradeItemsAsync(string tradeId)
    {
        return _cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await _repository.ListTradeItemsAsync(tradeId);
        },
        convertEntityToCachedEntity: (TradeItem entity) => _mapper.AdaptToType<TradeItem, CachedTradeItem>(entity),
        convertCachedEntityToEntity: (CachedTradeItem cachedEntity) => _mapper.AdaptToType<CachedTradeItem, TradeItem>(cachedEntity),
        true,
        (TradeItem tradeItem) =>
            tradeItem.ItemId
        );
    }

    public Task<string[]> GetTradeIdsUsingItemAsync(string itemId)
    {
        return _cacheService.GetSetValuesAsync(CacheKeys.UsedItem.GetUsedItemKey(itemId), async (args) =>
        {
            return await _repository.GetTradeIdsUsingItemAsync(itemId);
        },
        true);
    }

    public Task<bool> DeleteTradeItemsAsync(string tradeId) => _repository.DeleteTradeItemsAsync(tradeId);

    protected override string GetCacheKey(object entity)
    {
        if (entity is not TradeItem tradeContent) return string.Empty;

        return CacheKeys.TradeItem.GetTradeItemKey(tradeContent.TradeId, tradeContent.ItemId);
    }

    public void Dispose() => _repository.Dispose();
}
