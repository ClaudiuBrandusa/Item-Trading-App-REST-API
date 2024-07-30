using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.TradeItemHistory.AddTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Application.Behaviors.TradeItemHistory.RemoveTradeItems;
using Application.Extensions;
using Application.Results.TradeItemsHistory;
using Domain.Entities.Trades;
using Domain.Repositories.TradeItemsHistory;
using MapsterMapper;
using MediatR;

namespace Application.Services.TradeItemsHistory;

public class TradeItemHistoryService : ITradeItemHistoryService
{
    private readonly ICachedTradeItemHistoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly ISender _sender;

    public TradeItemHistoryService(ICachedTradeItemHistoryRepository repository, IMapper mapper, ISender sender)
    {
        _repository = repository;
        _mapper = mapper;
        _sender = sender;
    }

    public async Task<TradeItemHistoryResult> AddTradeItemsAsync(AddTradeItemsHistoryCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || model.TradeId.Length == 0) return new TradeItemHistoryResult
        {
            Errors = new string[] { "Invalid IDs" }
        };

        bool status = true;

        for (int i = 0; i < model.TradeItems.Length; i++)
        {
            string itemName = await GetItemNameAsync(model.TradeItems[i].ItemId);

            if (!await _repository.AddTradeItemHistoryAsync(model.TradeId, itemName, model.TradeItems[i]))
            {
                status = false;
                await RemoveTradeItemsAsync(new RemoveTradeItemsHistoryCommand { TradeId = model.TradeId });
                break;
            }
        }

        var result = new TradeItemHistoryResult
        {
            Success = status,
            TradeId = model.TradeId
        };

        if (!status)
            result.Errors = new string[] { "Unable to add trade items history" };

        await _repository.SaveChangesAsync();

        return result;
    }

    public async Task<TradeItem[]> GetTradeItemsAsync(GetTradeItemsHistoryQuery model)
    {
        if (string.IsNullOrEmpty(model.TradeId)) return Array.Empty<TradeItem>();

        string tradeId = model.TradeId;

        var array = await _repository.ListTradeItemHistoryAsync(tradeId);

        return array.Select(x => _mapper.AdaptToType<TradeItemHistory, TradeItem>(x)).ToArray();
    }

    public async Task<TradeItemHistoryResult> RemoveTradeItemsAsync(RemoveTradeItemsHistoryCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId)) return new TradeItemHistoryResult
        {
            Errors = new string[] { "Invalid IDs" }
        };

        string tradeId = model.TradeId;

        var status = await _repository.DeleteTradeItemHistoryForTradeAsync(tradeId) > 0;

        return status ?
            new TradeItemHistoryResult
            {
                Success = true,
                TradeId = tradeId
            } :
            new TradeItemHistoryResult
            {
                Errors = new string[] { "Unable to remove the trade items history" }
            };
    }

    private Task<string> GetItemNameAsync(string itemId) => _sender.Send(new GetItemNameQuery { ItemId = itemId });
}
