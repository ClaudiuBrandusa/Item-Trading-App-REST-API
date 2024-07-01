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
public class TradeItemRepository : RepositoryBase, ITradeItemRepository
{
    private readonly IMapper _mapper;

    public TradeItemRepository(IDatabaseContextWrapper databaseContextWrapper, ICacheService cacheService, IMapper mapper) : base(databaseContextWrapper, cacheService)
    {
        _mapper = mapper;
    }

    public Task<TradeContent?> GetTradeContentCachedAsync(string tradeId, string itemId)
    {
        return cacheService.GetEntityAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, itemId), async (args) =>
        {
            return await GetTradeContentAsync(tradeId, itemId);
        }, true);
    }

    public Task<TradeContent?> GetTradeContentAsync(string tradeId, string itemId)
    {
        return context.TradeContent
            .AsNoTracking()
            .Where(x => x.TradeId == tradeId && x.ItemId == itemId)
            .FirstOrDefaultAsync();
    }

    public Task<TradeContent[]> ListTradeContentsAsync(string tradeId)
    {
        return context.TradeContent
                .AsNoTracking()
                .Where(t => Equals(t.TradeId, tradeId))
                .ToArrayAsync();
    }

    public Task<TradeItem[]> ListTradeItemsCachedAsync(string tradeId, Func<string, Task<string>> getItemNameFunc)
    {
        return cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await ListTradeContentsAsync(tradeId);
        }, async (TradeContent content) =>
        {
            return _mapper.AdaptToType<TradeContent, TradeItem>(content, (nameof(TradeItem.Name), await getItemNameFunc(content.ItemId)));
        },
        true,
        (TradeItem tradeItem) =>
            tradeItem.ItemId
        );
    }

    public Task<string[]> GetTradeIdsUsingItemAsync(string itemId)
    {
        return context.TradeContent
            .AsNoTracking()
            .Where(x => x.ItemId == itemId)
            .Select(x => x.TradeId)
            .ToArrayAsync();
    }

    public Task<string[]> GetTradeIdsUsingItemCachedAsync(string itemId)
    {
        return cacheService.GetSetValuesAsync(CacheKeys.UsedItem.GetUsedItemKey(itemId), async (args) =>
        {
            return await GetTradeIdsUsingItemAsync(itemId);
        },
        true);
    }

    public async Task<bool> DeleteTradeItemsAsync(string tradeId)
    {
        var entitiesToBeDeleted = await context.TradeContent
            .Where(x => x.TradeId == tradeId)
            .ToListAsync();

        context.TradeContent
            .RemoveRange(entitiesToBeDeleted);

        return await context.SaveChangesAsync() > 0;
    }
}
