using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Application.Extensions;
using MapsterMapper;
using Domain.Entities.Trades;
using Domain.Repositories.TradeItemsHistory;
using Infrastructure.Data;

namespace Infrastructure.Repositories.TradeItems;

public class TradeItemHistoryRepository : RepositoryBase, ITradeItemHistoryRepository
{
    private readonly IMapper _mapper;

    public TradeItemHistoryRepository(IDatabaseContextWrapper databaseContextWrapper, IMapper mapper) : base(databaseContextWrapper)
    {
        _mapper = mapper;
    }

    public Task<bool> AddTradeItemHistoryAsync(string tradeId, string itemName, TradeItem tradeItem)
    {
        var entity = _mapper.AdaptToType<TradeItem, TradeItemHistory>(tradeItem, (nameof(TradeItemHistory.TradeId), tradeId), (nameof(TradeItemHistory.ItemName), itemName));

        return AddEntityAsync(entity);
    }

    public Task<TradeItemHistory?> GetTradeItemHistoryAsync(string tradeItemHistoryId) =>
        GetTradeItemHistoryQuery(context, tradeItemHistoryId);

    public Task<TradeItemHistory[]> ListTradeItemsHistoryAsync(string tradeId)
    {
        return context.TradeContentHistory
            .AsNoTracking()
            .Where(t => Equals(t.TradeId, tradeId))
            .ToArrayAsync();
    }

    public async Task<int> DeleteTradeItemsHistoryForTradeAsync(string tradeId)
    {
        var entitiesToBeDeleted = await context.TradeContentHistory
            .Where(x => x.TradeId == tradeId)
            .ToListAsync();

        context.TradeContentHistory
            .RemoveRange(entitiesToBeDeleted);

        return await context.SaveChangesAsync();
    }

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<TradeItemHistory?>> GetTradeItemHistoryQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeItemHistoryId) =>
            context.TradeContentHistory
                .AsNoTracking()
                .FirstOrDefault(x => x.ItemId == tradeItemHistoryId)
        );

    #endregion Queries
}
