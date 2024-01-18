using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.TradeItem;

public class TradeItemService : ITradeItemService
{
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public TradeItemService(DatabaseContext context, ICacheService cacheService, IMediator mediator, IMapper mapper)
    {
        _context = context;
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<bool> AddTradeItemAsync(AddTradeItemCommand model)
    {
        if (string.IsNullOrEmpty(model.ItemId) || string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.Name)) return false;

        if (model.Quantity < 1 || model.Price < 1) return false;

        var tradeContent = _mapper.AdaptToType<AddTradeItemCommand, TradeContent>(model);

        await _context.AddAsync(tradeContent);

        await _cacheService.SetCacheValueAsync(CacheKeys.TradeItem.GetTradeItemKey(model.TradeId, model.ItemId), model);
        await _cacheService.AddToSet(CacheKeys.UsedItem.GetUsedItemKey(model.ItemId), model.TradeId);

        return true;
    }

    public Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsQuery model)
    {
        return GetTradeItemsAsync(model.TradeId);
    }

    public Task<string[]> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model)
    {
        return _cacheService.GetSetValuesAsync(CacheKeys.UsedItem.GetUsedItemKey(model.ItemId), async (args) =>
        {
            return await _context
                .TradeContent
                .AsNoTracking()
                .Where(x => x.ItemId == model.ItemId)
                .Select(x => x.TradeId)
                .ToArrayAsync();
        },
        true);
    }

    private Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(string tradeId)
    {
        return _cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await _context.TradeContent.AsNoTracking().Where(t => Equals(t.TradeId, tradeId)).ToArrayAsync();
        }, async (TradeContent content) =>
        {
            return _mapper.AdaptToType<TradeContent, Models.TradeItems.TradeItem>(content, (nameof(Models.TradeItems.TradeItem.Name), await GetItemNameAsync(content.ItemId)));
        },
        true,
        (Models.TradeItems.TradeItem tradeItem) =>
            tradeItem.ItemId
        );
    }

    private Task<string> GetItemNameAsync(string itemId) => _mediator.Send(new GetItemNameQuery { ItemId = itemId });
}
