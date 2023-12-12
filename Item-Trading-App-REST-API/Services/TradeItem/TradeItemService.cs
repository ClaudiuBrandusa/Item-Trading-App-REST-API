using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.Item;
using Item_Trading_App_REST_API.Requests.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

    public async Task<bool> AddTradeItemAsync(AddTradeItemRequest model)
    {
        if (string.IsNullOrEmpty(model.ItemId) || string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.Name)) return false;

        if (model.Quantity < 1 || model.Price < 1) return false;

        var tradeContent = _mapper.AdaptToType<AddTradeItemRequest, TradeContent>(model);

        await _context.TradeContent.AddAsync(tradeContent);
        
        await _cacheService.SetCacheValueAsync(CacheKeys.TradeItem.GetTradeItemKey(model.TradeId, model.ItemId), model);
        await _cacheService.AddToSet(CacheKeys.UsedItem.GetUsedItemKey(model.ItemId), model.TradeId);

        return true;
    }

    public async Task<List<Models.Trade.TradeItem>> GetTradeItemsAsync(string tradeId)
    {
        return await GetTradeContentAsAsync(tradeId,
            (TradeContent content) =>
                _mapper.AdaptToType<TradeContent, Models.Trade.TradeItem>(content),
            (Models.Trade.TradeItem tradeItem) =>
                tradeItem.ItemId
            );
    }

    public Task<List<ItemPrice>> GetItemPricesAsync(GetItemPricesQuery model)
    {
        return GetItemPricesAsync(model.TradeId);
    }

    public Task<List<string>> GetItemTradeIdsAsync(string itemId)
    {
        return _cacheService.GetSetValuesAsync(CacheKeys.UsedItem.GetUsedItemKey(itemId), async (args) =>
        {
            return await _context
                .TradeContent
                .AsNoTracking()
                .Where(x => x.ItemId == itemId)
                .Select(x => x.TradeId)
                .ToListAsync();
        },
        true);
    }

    private async Task<List<ItemPrice>> GetItemPricesAsync(string tradeId)
    {
        return await GetTradeContentAsAsync(tradeId,
            async (TradeContent content) =>
                _mapper.AdaptToType<TradeContent, ItemPrice>(content, (nameof(ItemPrice.Name), await GetItemNameAsync(content.ItemId))
            ),
            (itemPrice) =>
                itemPrice.ItemId
            );
    }

    private Task<List<T>> GetTradeContentAsAsync<T>(string tradeId, Func<TradeContent, Task<T>> mapFromTradeContentFunc, Func<T, string> getEntityIdFunc) where T : class
    {
        return _cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await _context.TradeContent.AsNoTracking().Where(t => Equals(t.TradeId, tradeId)).ToListAsync();
        }, async (TradeContent content) =>
        {
            return await mapFromTradeContentFunc(content);
        }, true, (T entity) => getEntityIdFunc(entity));
    }

    private Task<List<T>> GetTradeContentAsAsync<T>(string tradeId, Func<TradeContent, T> mapFromTradeContentFunc, Func<T, string> getEntityIdFunc) where T : class
    {
        return _cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await _context.TradeContent.AsNoTracking().Where(t => Equals(t.TradeId, tradeId)).ToListAsync();
        }, (TradeContent content) =>
        {
            return Task.FromResult(mapFromTradeContentFunc(content));
        }, true, (T entity) => getEntityIdFunc(entity));
    }

    private Task<string> GetItemNameAsync(string itemId) => _mediator.Send(new GetItemNameQuery { ItemId = itemId });
}
