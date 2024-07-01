using Application.Constants;
using Application.Services.Cache;
using Domain.Trades;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Application.Extensions;
using MapsterMapper;
using Domain.Repositories;
using Domain.TradeItems;

namespace Infrastructure.Repositories;

public class TradeItemHistoryRepository : RepositoryBase, ITradeItemHistoryRepository
{
    private readonly IMapper _mapper;

    public TradeItemHistoryRepository(IDatabaseContextWrapper databaseContextWrapper, ICacheService cacheService, IMapper mapper) : base(databaseContextWrapper, cacheService)
    {
        _mapper = mapper;
    }

    public Task<bool> AddTradeItemHistoryAsync(string tradeId, TradeItem tradeItem)
    {
        var entity = _mapper.AdaptToType<TradeItem, TradeContentHistory>(tradeItem, (nameof(TradeContentHistory.TradeId), tradeId));

        return AddEntityAsync(entity);
    }

    public Task<TradeItem[]> ListTradeContentHistoryAsTradeItemCachedAsync(string tradeId)
    {
        return cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await ListTradeContentHistoryAsync(tradeId);
        }, (TradeContentHistory content) =>
        {
            return Task.FromResult(_mapper.AdaptToType<TradeContentHistory, TradeItem>(content));
        },
            true,
            (TradeItem tradeItem) =>
                tradeItem.ItemId
        );
    }

    public Task<TradeContentHistory[]> ListTradeContentHistoryAsync(string tradeId)
    {
        return context.TradeContentHistory
            .AsNoTracking()
            .Where(t => Equals(t.TradeId, tradeId))
            .ToArrayAsync();
    }

    public async Task<int> DeleteTradeContentHistoryForTradeAsync(string tradeId)
    {
        var entitiesToBeDeleted = await context.TradeContentHistory
            .Where(x => x.TradeId == tradeId)
            .ToListAsync();

        context.TradeContentHistory
            .RemoveRange(entitiesToBeDeleted);

        return await context.SaveChangesAsync();
    }
}
