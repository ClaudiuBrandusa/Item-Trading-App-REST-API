using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Cache;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using MapsterMapper;
using Item_Trading_App_REST_API.Resources.Events.Item;

namespace Item_Trading_App_REST_API.Services.Item;

public class ItemService : IItemService
{
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ItemService(DatabaseContext context, ICacheService cacheService, IMediator mediator, IMapper mapper)
    {
        _context = context;
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<FullItemResult> CreateItemAsync(CreateItemCommand model)
    {
        if(model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.ItemName))
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = _mapper.AdaptToType<CreateItemCommand, Entities.Item>(model, (nameof(Entities.Item.ItemId), Guid.NewGuid().ToString()));

        if (!await _context.AddEntityAsync(item))
            return new FullItemResult
            {
                Errors = new[] { "Unable to add this item" }
            };

        await Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.Item.GetItemKey(item.ItemId), item),
            _mediator.Publish(new ItemCreatedEvent { Item = item, SenderUserId = model.SenderUserId })
        );
        
        return new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description,
            Success = true
        };
    }

    public async Task<FullItemResult> UpdateItemAsync(UpdateItemCommand model)
    {
        if(model is null || string.IsNullOrEmpty(model.ItemName))
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(model.ItemId),
            (args) => GetItemEntityAsync(model.ItemId));

        if (item is null)
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        item.Name = model.ItemName;        
        item.Description = model.ItemDescription;

        if (!await _context.UpdateEntityAsync(item))
            return new FullItemResult
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                ItemDescription = item.Description,
                Errors = new[] { "Unable to update item" }
            };

        await Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.Item.GetItemKey(item.ItemId), item),
            _mediator.Publish(new ItemUpdatedEvent { Item = item, SenderUserId = model.SenderUserId })   
        );

        return new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description,
            Success = true
        };
    }

    public async Task<DeleteItemResult> DeleteItemAsync(DeleteItemCommand model)
    {
        if(string.IsNullOrEmpty(model.ItemId))
            return new DeleteItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var isUsedInATrade = await _mediator.Send(new ItemUsedInTradeQuery { ItemId = model.ItemId });

        if (isUsedInATrade)
            return new DeleteItemResult
            {
                Errors = new[] { "Unable to delete an item that is used in a trade" }
            };

        string cacheKey = CacheKeys.Item.GetItemKey(model.ItemId);

        var item = await _cacheService.GetEntityAsync(
            cacheKey,
            (args) => GetItemEntityAsync(model.ItemId));

        if (item is null)
            return new DeleteItemResult
            { 
                Errors = new[] { "Something went wrong" }
            };
        else
            await _cacheService.ClearCacheKeyAsync(cacheKey);

        if(!await _context.RemoveEntityAsync(item))
            return new DeleteItemResult
            {
                Errors = new[] { "Unable to remove item" }
            };

        await _mediator.Publish(new ItemDeletedEvent { ItemId = model.ItemId, UserId = model.UserId });
        
        return new DeleteItemResult
        {
            ItemId = model.ItemId,
            ItemName = item.Name,
            Success = true
        };
    }

    public async Task<FullItemResult> GetItemAsync(GetItemQuery model)
    {
        if(string.IsNullOrEmpty(model.ItemId))
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(model.ItemId),
            (args) => GetItemEntityAsync(model.ItemId),
            true);

        if (item is null)
            return new FullItemResult
            {
                ItemId = model.ItemId,
                Errors = new[] { "Item not found" }
            };

        return new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description,
            Success = true
        };
    }

    public async Task<ItemsResult> ListItemsAsync(ListItemsQuery model)
    {
        var items = await _cacheService.GetEntitiesAsync(
            CacheKeys.Item.GetItemsKey(),
            (args) => _context.Items.AsNoTracking().ToArrayAsync(),
            true,
            (Entities.Item x) => x.ItemId);

        if (!string.IsNullOrEmpty(model.SearchString))
            items = items.Where(x => x.Name.ToLower().StartsWith(model.SearchString.ToLower())).ToArray();

        return new ItemsResult
        {
            ItemsId = items.Select(i => i.ItemId),
            Success = true
        };
    }

    public async Task<string> GetItemNameAsync(GetItemNameQuery model)
    {
        var entity = await _cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(model.ItemId),
            (args) => GetItemEntityAsync(model.ItemId),
            true);

        return entity?.Name ?? "";
    }

    public async Task<string> GetItemDescriptionAsync(GetItemDescriptionQuery model)
    {
        var entity = await _cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(model.ItemId),
            (args) => GetItemEntityAsync(model.ItemId),
            true);

        return entity?.Description ?? "";
    }

    private Task<Entities.Item> GetItemEntityAsync(string itemId) =>
        GetItemQuery(_context, itemId);

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Entities.Item>> GetItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string itemId) =>
            context.Items
                .AsNoTracking()
                .FirstOrDefault(x => x.ItemId == itemId)
        );

    #endregion Queries
}
