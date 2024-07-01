using Application.Constants;
using Application.Services.Cache;
using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Application.Extensions;
using MediatR;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Domain.Repositories;
using Domain.Trade;
using Domain.TradeItems;

namespace Infrastructure.Repositories;
public class TradeRepository : RepositoryBase, ITradeRepository
{
    private readonly ISender _sender;

    public TradeRepository(IDatabaseContextWrapper databaseContextWrapper, ICacheService cacheService, ISender sender) : base(databaseContextWrapper, cacheService)
    {
        _sender = sender;
    }

    public Task<CachedTrade> GetCachedTradeAsync(string tradeId)
    {
        return cacheService.GetEntityAsync(
            CacheKeys.Trade.GetTradeKey(tradeId),
            async (args) =>
            {
                var tradeTask = GetTradeEntityAsync(tradeId);
                var sentTradeTask = GetSentTradeEntityAsync(tradeId);
                var receivedTradeTask = GetReceivedTradeEntityAsync(tradeId);

                var trade = await tradeTask;

                var tradeItems = await GetTradeItemsAsync(trade.TradeId, trade.Response.HasValue /* if trade.Response has value, then it means it is a responded trade */ );

                var tradeItemIds = tradeItems.Select(tradeItem => tradeItem.ItemId).ToArray();

                return new CachedTrade
                {
                    TradeId = trade.TradeId,
                    SenderUserId = (await sentTradeTask)?.SenderId ?? "",
                    ReceiverUserId = (await receivedTradeTask)?.ReceiverId ?? "",
                    SentDate = trade.SentDate,
                    Response = trade.Response,
                    ResponseDate = trade.ResponseDate,
                    TradeItemsId = tradeItemIds
                };
            },
            true
        );
    }

    public async ValueTask AddSentAndReceivedTradeEntitiesAsync(string tradeId, string senderUserId, string receiverUserId)
    {
        var sentTradeTask = context.AddAsync(
            new Domain.Trades.SentTrade
            {
                TradeId = tradeId,
                SenderId = senderUserId
            });
        var receivedTradeTask = context.AddAsync(
            new Domain.Trades.ReceivedTrade
            {
                TradeId = tradeId,
                ReceiverId = receiverUserId
            });

        await sentTradeTask;
        await receivedTradeTask;
    }

    public Task<TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded) => responded ?
        _sender.Send(new GetTradeItemsHistoryQuery { TradeId = tradeId }) :
        _sender.Send(new GetTradeItemsQuery { TradeId = tradeId });

    public async Task<Domain.Trades.Trade?> GetTradeEntityAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var tradeEntity = await GetTradeQuery(dbContext, tradeId);

        DatabaseContextWrapper.Dispose(dbContext);

        return tradeEntity;
    }

    public async Task<Domain.Trades.SentTrade?> GetSentTradeEntityAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var sentTradeEntity = await GetSentTradeQuery(dbContext, tradeId);

        DatabaseContextWrapper.Dispose(dbContext);

        return sentTradeEntity;
    }

    public async Task<Domain.Trades.ReceivedTrade?> GetReceivedTradeEntityAsync(string tradeId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var receivedTradeEntity = await GetReceivedTradeQuery(dbContext, tradeId);

        DatabaseContextWrapper.Dispose(dbContext);

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

    public Task<string[]> ListReceivedTradeIdsCachedAsync(string userId)
    {
        return cacheService.GetEntityIdsAsync(
            CacheKeys.Trade.GetReceivedTradeKey(userId, ""),
            async (args) => await ListReceivedTradeIdsAsync(userId),
            true
        );
    }

    public Task<string[]> ListSentTradeIdsCachedAsync(string userId)
    {
        return cacheService.GetEntityIdsAsync(
            CacheKeys.Trade.GetSentTradeKey(userId, ""),
            async (args) => await ListSentTradeIdsAsync(userId),
            true
        );
    }

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Domain.Trades.Trade?>> GetTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.Trades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    private static readonly Func<DatabaseContext, string, Task<Domain.Trades.SentTrade?>> GetSentTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.SentTrades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    private static readonly Func<DatabaseContext, string, Task<Domain.Trades.ReceivedTrade?>> GetReceivedTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.ReceivedTrades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    #endregion Queries
}
