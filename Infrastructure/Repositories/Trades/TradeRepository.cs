using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Domain.Entities.Trades;
using Domain.Aggregates.Trades;
using Domain.Repositories.Trades;
using Application.Behaviors.Item.GetItemName;

namespace Infrastructure.Repositories.Trades;
public class TradeRepository : RepositoryBase, ITradeRepository
{
    private readonly ISender _sender;

    public TradeRepository(IDatabaseContextWrapper databaseContextWrapper, ISender sender) : base(databaseContextWrapper)
    {
        _sender = sender;
    }

    public async Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded)
    {
        if (responded)
        {
            return (await GetTradeItemHistoryQuery(context, tradeId)).Select(x => new TradeItem(tradeId, x.ItemId, x.Quantity, x.Price)).ToArray();
        }
        
        var trade = await GetTradeQuery(context, tradeId);

        return trade!.TradeContents.ToArray();
    }

    public async Task<Trade?> GetTradeAsync(string tradeId)
    {
        var trade = await GetTradeTrackingQuery(context, tradeId);
        
        return trade;
    }

    public async Task<bool?> GetTradeResponseAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var response = await GetTradeResponseQuery(dbContext, tradeId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return response;
    }

    public async Task<Trade?> GetTradeEntityAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var tradeEntity = await GetTradeQuery(dbContext, tradeId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return tradeEntity;
    }

    public Task<string[]> GetTradeIdsUsingItemAsync(string itemId)
    {
        return context.TradeContent
            .AsNoTracking()
            .Where(x => x.ItemId == itemId)
            .Select(x => x.TradeId)
            .ToArrayAsync();
    }

    public Task<string[]> ListReceivedTradeIdsAsync(string userId)
    {
        return context.ReceivedTrades
                .AsNoTracking()
                .Where(o => Equals(userId, o.ReceiverId))
                .Select(t => t.TradeId)
                .ToArrayAsync();
    }

    public Task<string[]> ListSentTradeIdsAsync(string userId)
    {
        return context.SentTrades
                .AsNoTracking()
                .Where(st => Equals(st.SenderId, userId))
                .Select(t => t.TradeId)
                .ToArrayAsync();
    }

    public async Task<bool> HasTradeItemAsync(string tradeId, string itemId)
    {
        return await HasTradeItemQuery(context, tradeId, itemId);
    }

    public async Task<bool> HasTradeItemHistoryAsync(string tradeId, string itemId)
    {
        return await HasTradeItemHistoryQuery(context, tradeId, itemId);
    }

    public async Task<bool> IsItemUsedInTrade(string itemId)
    {
        return await IsItemUsedInTradeQuery(context, itemId);
    }

    public async Task<bool> MoveTradeContentToHistory(string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId, false);

        var itemsAmount = tradeItems.Length;

        var tradeItemsHistory = new TradeItemHistory[itemsAmount];

        for (int i = 0; i < itemsAmount; i++)
        {
            var tradeItem = tradeItems[i];

            var itemName = await _sender.Send(new GetItemNameQuery{ ItemId = tradeItem.ItemId });

            tradeItemsHistory[i] = new TradeItemHistory(tradeId, tradeItem.ItemId, itemName, tradeItem.Quantity, tradeItem.Price);
        }

        bool failed = false;

        foreach (var tradeItemHistory in tradeItemsHistory)
        {
            if (failed)
            {
                return false;
            }

            failed = !await AddEntityAsync(tradeItemHistory);
        }

        return !failed;
    }

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Trade?>> GetTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.Trades
                .AsNoTracking()
                .Include(t => t.SentTrade)
                .Include(t => t.ReceivedTrade)
                .Include(t => t.TradeContents)
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    private static readonly Func<DatabaseContext, string, Task<bool?>> GetTradeResponseQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.Trades
                .AsNoTracking()
                .Where(t => t.TradeId == tradeId)
                .Select(t => t.Response)
                .FirstOrDefault()
        );

    private static readonly Func<DatabaseContext, string, Task<Trade?>> GetTradeTrackingQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.Trades
                .Include(t => t.SentTrade)
                .Include(t => t.ReceivedTrade)
                .Include(t => t.TradeContents)
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    private static readonly Func<DatabaseContext, string, Task<SentTrade?>> GetSentTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.SentTrades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    private static readonly Func<DatabaseContext, string, Task<ReceivedTrade?>> GetReceivedTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.ReceivedTrades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    private static readonly Func<DatabaseContext, string, string, Task<bool>> HasTradeItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId, string itemId) =>
            context.TradeContent
                .AsNoTracking()
                .Where(tc => tc.TradeId == tradeId)
                .Any(tc => tc.ItemId == itemId)
        );

    private static readonly Func<DatabaseContext, string, string, Task<bool>> HasTradeItemHistoryQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId, string itemId) =>
            context.TradeContentHistory
                .AsNoTracking()
                .Where(tc => tc.TradeId == tradeId)
                .Any(tc => tc.ItemId == itemId)
        );

    private static readonly Func<DatabaseContext, string, Task<TradeItemHistory[]>> GetTradeItemHistoryQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.TradeContentHistory
                .AsNoTracking()
                .Where(tc => tc.TradeId == tradeId)
                .ToArray()
        );

    private static readonly Func<DatabaseContext, string, Task<bool>> IsItemUsedInTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string itemId) =>
            context.TradeContent
                .AsNoTracking()
                .Any(tc => tc.ItemId == itemId)
        );

    #endregion Queries
}
