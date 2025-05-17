using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Domain.Entities.Trades;
using Domain.Aggregates.Trades;
using Domain.Repositories.Trades;

namespace Infrastructure.Repositories.Trades;
public class TradeRepository : RepositoryBase, ITradeRepository
{
    private readonly ISender _sender;

    public TradeRepository(IDatabaseContextWrapper databaseContextWrapper, ISender sender) : base(databaseContextWrapper)
    {
        _sender = sender;
    }

    public async ValueTask AddSentAndReceivedTradeEntitiesAsync(string tradeId, string senderUserId, string receiverUserId)
    {
        var sentTradeTask = context.AddAsync(
            new SentTrade(tradeId, senderUserId));
        var receivedTradeTask = context.AddAsync(
            new ReceivedTrade(tradeId, receiverUserId)
        );

        await sentTradeTask;
        await receivedTradeTask;
    }

    public Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded) => responded ?
        _sender.Send(new GetTradeItemsHistoryQuery { TradeId = tradeId }) :
        _sender.Send(new GetTradeItemsQuery { TradeId = tradeId });

    public async Task<Trade?> GetTradeEntityAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var tradeEntity = await GetTradeQuery(dbContext, tradeId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return tradeEntity;
    }

    public async Task<SentTrade?> GetSentTradeEntityAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var sentTradeEntity = await GetSentTradeQuery(dbContext, tradeId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return sentTradeEntity;
    }

    public async Task<ReceivedTrade?> GetReceivedTradeEntityAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var receivedTradeEntity = await GetReceivedTradeQuery(dbContext, tradeId);

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return receivedTradeEntity;
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

    #endregion Queries
}
