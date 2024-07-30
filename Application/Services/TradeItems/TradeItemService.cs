using Application.Behaviors.TradeItem.AddTradeItem;
using Application.Behaviors.TradeItem.GetTradeItemIds;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItem.HasTradeItem;
using Application.Behaviors.TradeItem.RemoveTradeItems;
using Application.Extensions;
using Domain.Entities.Trades;
using Domain.Repositories.TradeItems;
using MapsterMapper;
using MediatR;

namespace Application.Services.TradeItems;

public class TradeItemService : ITradeItemService, IDisposable
{
    private readonly ICachedTradeItemRepository _repository;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public TradeItemService(ICachedTradeItemRepository repository, IMediator mediator, IMapper mapper)
    {
        _repository = repository;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<bool> AddTradeItemAsync(AddTradeItemCommand model)
    {
        if (string.IsNullOrEmpty(model.ItemId) || string.IsNullOrEmpty(model.TradeId)) return false;

        if (model.Quantity < 1 || model.Price < 1) return false;

        var tradeContent = _mapper.AdaptToType<AddTradeItemCommand, TradeItem>(model);

        await _repository.AddEntityAsync(tradeContent);

        await TradeItemCreated(tradeContent, model.TradeId);

        return true;
    }

    public async Task<bool> HasTradeItemAsync(HasTradeItemQuery model)
    {
        return await _repository.GetTradeItemAsync(model.TradeId, model.ItemId) is not default(Domain.Entities.Trades.TradeItem);
    }

    public Task<TradeItem[]> GetTradeItemAsync(GetTradeItemsQuery model)
    {
        return GetTradeItemsAsync(model.TradeId);
    }

    public Task<string[]> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model)
    {
        return _repository.GetTradeIdsUsingItemAsync(model.ItemId);
    }
    public async Task<bool> RemoveTradeItemsAsync(RemoveTradeItemsCommand model)
    {
        var result = await _repository.DeleteTradeItemsAsync(model.TradeId);

        if (!result) return false;

        await TradeItemRemoved(model.TradeId, model.KeepCache);

        return true;
    }

    public void Dispose()
    {
        _repository.Dispose();
        GC.SuppressFinalize(this);
    }

    private Task<TradeItem[]> GetTradeItemsAsync(string tradeId)
    {
        return _repository.ListTradeItemsAsync(tradeId);
    }

    private Task TradeItemCreated(TradeItem tradeItem, string tradeId)
    {
        var eventNotification = new TradeItemAddedEvent
        {
            TradeId = tradeId,
            Data = tradeItem
        };

        return _mediator.Publish(eventNotification);
    }

    private Task TradeItemRemoved(string tradeId, bool keepCache)
    {
        var eventNotification = new TradeItemRemovedEvent
        {
            TradeId = tradeId,
            KeepCache = keepCache
        };

        return _mediator.Publish(eventNotification);
    }
}
