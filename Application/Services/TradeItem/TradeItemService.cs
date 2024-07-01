using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.TradeItem.AddTradeItem;
using Application.Behaviors.TradeItem.GetTradeItemIds;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItem.HasTradeItem;
using Application.Behaviors.TradeItem.RemoveTradeItems;
using Application.Extensions;
using Application.Services.Cache;
using Domain.Repositories;
using Domain.Trades;
using MapsterMapper;
using MediatR;

namespace Application.Services.TradeItem;

public class TradeItemService : ITradeItemService, IDisposable
{
    private readonly ITradeItemRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public TradeItemService(ITradeItemRepository repository, ICacheService cacheService, IMediator mediator, IMapper mapper)
    {
        _repository = repository;
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<bool> AddTradeItemAsync(AddTradeItemCommand model)
    {
        if (string.IsNullOrEmpty(model.ItemId) || string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.Name)) return false;

        if (model.Quantity < 1 || model.Price < 1) return false;

        var tradeContent = _mapper.AdaptToType<AddTradeItemCommand, TradeContent>(model);

        var itemNameTask = GetItemNameAsync(model.ItemId);

        await _repository.AddEntityAsync(tradeContent);

        await TradeItemCreated(tradeContent, await itemNameTask, model.TradeId);

        return true;
    }

    public async Task<bool> HasTradeItemAsync(HasTradeItemQuery model)
    {
        return await _repository.GetTradeContentCachedAsync(model.TradeId, model.ItemId) is not default(TradeContent);
    }

    public Task<Domain.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsQuery model)
    {
        return GetTradeItemsAsync(model.TradeId);
    }

    public Task<string[]> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model)
    {
        return _repository.GetTradeIdsUsingItemCachedAsync(model.ItemId);
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

    private Task<Domain.TradeItems.TradeItem[]> GetTradeItemsAsync(string tradeId)
    {
        return _repository.ListTradeItemsCachedAsync(tradeId, GetItemNameAsync);
    }

    private Task TradeItemCreated(TradeContent tradeContent, string itemName, string tradeId)
    {
        var tradeItem = _mapper.AdaptToType<TradeContent, Domain.TradeItems.TradeItem>(tradeContent, (nameof(Domain.TradeItems.TradeItem.Name), itemName));

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

    private Task<string> GetItemNameAsync(string itemId) => _mediator.Send(new GetItemNameQuery { ItemId = itemId });
}
