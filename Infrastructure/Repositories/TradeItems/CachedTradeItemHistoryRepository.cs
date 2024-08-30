using Application.Constants;
using Application.Services.Cache;
using Application.Extensions;
using Domain.Entities.Trades;
using MapsterMapper;
using Application.Models.TradeItems;
using Domain.Repositories.TradeItemsHistory;
using Application.Repositories;

namespace Infrastructure.Repositories.TradeItems;

public class CachedTradeItemHistoryRepository : CachedRepository, ICachedTradeItemHistoryRepository
{
    private readonly ITradeItemHistoryRepository _repository;
    private readonly IMapper _mapper;

    public CachedTradeItemHistoryRepository(ITradeItemHistoryRepository repository, ICacheService cacheService, IMapper mapper) : base(repository, cacheService)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public Task<bool> AddTradeItemHistoryAsync(string tradeId, string itemName, TradeItem tradeItem) => _repository.AddTradeItemHistoryAsync(tradeId, itemName, tradeItem);

    public Task<TradeItemHistory[]> ListTradeItemHistoryAsync(string tradeId)
    {
        return _cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await _repository.ListTradeItemsHistoryAsync(tradeId);
        },
        convertEntityToCachedEntity: (TradeItemHistory entity) => _mapper.AdaptToType<TradeItemHistory, CachedTradeItemHistory>(entity),
        convertCachedEntityToEntity: (CachedTradeItemHistory cachedEntity) => _mapper.AdaptToType<CachedTradeItemHistory, TradeItemHistory>(cachedEntity),
        true,
        (TradeItem tradeItem) =>
            tradeItem.ItemId
        );
    }

    public async Task<int> DeleteTradeItemHistoryForTradeAsync(string tradeId)
    {
        await _cacheService.ClearCacheKeyAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""));

        return await _repository.DeleteTradeItemsHistoryForTradeAsync(tradeId);
    }

    protected override string GetCacheKey(object entity)
    {
        if (entity is not TradeItemHistory tch)
            return string.Empty;

        return CacheKeys.TradeItem.GetTradeItemKey(tch.TradeId, "");
    }

    public void Dispose() => _repository.Dispose();
}
