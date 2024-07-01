using Application.Behaviors.TradeItemHistory.AddTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Application.Behaviors.TradeItemHistory.RemoveTradeItems;
using Application.Constants;
using Application.Models.TradeItemHistory;
using Application.Services.Cache;
using Domain.Repositories;

namespace Application.Services.TradeItemHistory;

public class TradeItemHistoryService : ITradeItemHistoryService
{
    private readonly ITradeItemHistoryRepository _repository;
    private readonly ICacheService _cacheService;

    public TradeItemHistoryService(ITradeItemHistoryRepository repository, ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<TradeItemHistoryBaseResult> AddTradeItemsAsync(AddTradeItemsHistoryCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || model.TradeId.Length == 0) return new TradeItemHistoryBaseResult
        {
            Errors = new string[] { "Invalid IDs" }
        };

        bool status = true;

        for (int i = 0; i < model.TradeItems.Length; i++)
        {
            if (!await _repository.AddTradeItemHistoryAsync(model.TradeId, model.TradeItems[i]))
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

        await _repository.SaveChangesAsync();

        return result;
    }

    public async Task<Domain.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsHistoryQuery model)
    {
        if (string.IsNullOrEmpty(model.TradeId)) return Array.Empty<Domain.TradeItems.TradeItem>();

        string tradeId = model.TradeId;

        return await _repository.ListTradeContentHistoryAsTradeItemCachedAsync(tradeId);
    }

    public async Task<TradeItemHistoryBaseResult> RemoveTradeItemsAsync(RemoveTradeItemsHistoryCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId)) return new TradeItemHistoryBaseResult
        {
            Errors = new string[] { "Invalid IDs" }
        };

        string tradeId = model.TradeId;
        await _cacheService.ClearCacheKeyAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""));

        var status = await _repository.DeleteTradeContentHistoryForTradeAsync(tradeId) > 0;

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
}
