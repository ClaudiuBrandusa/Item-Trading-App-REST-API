using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using MapsterMapper;
using Domain.Entities.Trades;
using Domain.Repositories.TradeItems;

namespace Infrastructure.Repositories.TradeItems;
public class TradeContentRepository : RepositoryBase, ITradeItemRepository
{
    private readonly IMapper _mapper;

    public TradeContentRepository(IDatabaseContextWrapper databaseContextWrapper, IMapper mapper) : base(databaseContextWrapper)
    {
        _mapper = mapper;
    }

    public Task<TradeItem?> GetTradeItemAsync(string tradeId, string itemId)
    {
        return context.TradeContent
            .AsNoTracking()
            .Where(x => x.TradeId == tradeId && x.ItemId == itemId)
            .FirstOrDefaultAsync();
    }

    public Task<TradeItem[]> ListTradeItemsAsync(string tradeId)
    {
        return context.TradeContent
                .AsNoTracking()
                .Where(t => Equals(t.TradeId, tradeId))
                .ToArrayAsync();
    }

    public Task<string[]> GetTradeIdsUsingItemAsync(string itemId)
    {
        return context.TradeContent
            .AsNoTracking()
            .Where(x => x.ItemId == itemId)
            .Select(x => x.TradeId)
            .ToArrayAsync();
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
