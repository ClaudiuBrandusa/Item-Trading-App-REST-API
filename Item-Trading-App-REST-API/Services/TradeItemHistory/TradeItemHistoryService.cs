using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Models.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Queries.TradeItemHistory;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.UnitOfWork;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.TradeItemHistory;

public class TradeItemHistoryService : ITradeItemHistoryService
{
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    public TradeItemHistoryService(IDbContextFactory<DatabaseContext> dbContextFactory, ICacheService cacheService, IMapper mapper, IUnitOfWorkService unitOfWork)
    {
        _context = dbContextFactory.CreateDbContext();
        _cacheService = cacheService;
        _mapper = mapper;

        if (unitOfWork.Transaction is not null)
            _context.Database.UseTransaction(unitOfWork.Transaction.GetDbTransaction());
    }

    public async Task<TradeItemHistoryBaseResult> AddTradeItemsAsync(AddTradeItemsHistoryCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || model.TradeId.Length == 0) return new TradeItemHistoryBaseResult
        {
            Errors = new string[] { "Invalid IDs" }
        };

        bool status = true;

        for(int i = 0; i < model.TradeItems.Length; i++)
        {
            if (!await AddTradeItemAsync(model.TradeId, model.TradeItems[i]))
            {
                status = false;
                await RemoveTradeItemsAsync(new RemoveTradeItemsHistoryCommand { TradeId = model.TradeId });
                break;
            }
        }

        var result = new TradeItemHistoryBaseResult
        {
            Success = status,
            TradeId = model.TradeId
        };

        if (!status)
            result.Errors = new string[] { "Unable to add trade items history" };

        return result;
    }

    public async Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsHistoryQuery model)
    {
        if (string.IsNullOrEmpty(model.TradeId)) return Array.Empty<Models.TradeItems.TradeItem>();

        string tradeId = model.TradeId;

        return await _cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await _context.TradeContentHistory.AsNoTracking().Where(t => Equals(t.TradeId, tradeId)).ToArrayAsync();
        }, (TradeContentHistory content) =>
        {
            return Task.FromResult(_mapper.AdaptToType<TradeContentHistory, Models.TradeItems.TradeItem>(content));
        },
        true,
        (Models.TradeItems.TradeItem tradeItem) =>
            tradeItem.ItemId
        );
    }

    public async Task<TradeItemHistoryBaseResult> RemoveTradeItemsAsync(RemoveTradeItemsHistoryCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId)) return new TradeItemHistoryBaseResult
        {
            Errors = new string[] { "Invalid IDs" }
        };

        string tradeId = model.TradeId;
        await _cacheService.ClearCacheKeyAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""));

        var status = await _context.TradeContentHistory
            .Where(tc => tc.TradeId == tradeId)
            .ExecuteDeleteAsync() > 0;

        return status ?
            new TradeItemHistoryBaseResult
            {
                Success = true,
                TradeId = tradeId
            } :
            new TradeItemHistoryBaseResult
            {
                Errors = new string[] { "Unable to remove the trade items history" }
            };
    }

    #region Private

    private async Task<bool> AddTradeItemAsync(string tradeId, Models.TradeItems.TradeItem tradeItem)
    {
        var result = await _context.AddEntityAsync(_mapper.AdaptToType<Models.TradeItems.TradeItem, TradeContentHistory>(tradeItem, (nameof(TradeContentHistory.TradeId), tradeId)));

        return result;
    }

    #endregion Private
}
