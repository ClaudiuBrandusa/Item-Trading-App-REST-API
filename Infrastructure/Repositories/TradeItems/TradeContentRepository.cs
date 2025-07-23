using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Trades;
using Domain.Repositories.TradeItems;

namespace Infrastructure.Repositories.TradeItems;
public class TradeContentRepository : RepositoryBase, ITradeItemRepository
{
    public TradeContentRepository(IDatabaseContextWrapper databaseContextWrapper) : base(databaseContextWrapper)
    {
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
