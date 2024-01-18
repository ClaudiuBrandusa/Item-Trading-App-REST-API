using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using MapsterMapper;

namespace Item_Trading_App_REST_API.Services.Item;

public class ItemService : IItemService
{
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly IClientNotificationService _clientNotificationService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ItemService(DatabaseContext context, ICacheService cacheService, IClientNotificationService clientNotificationService, IMediator mediator, IMapper mapper)
    {
        _context = context;
        _cacheService = cacheService;
        _clientNotificationService = clientNotificationService;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<FullItemResult> CreateItemAsync(CreateItemCommand model)
    {
        if(model is null)
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

        await SetItemCacheAsync(item.ItemId, item);
        await _clientNotificationService.SendCreatedNotificationToAllUsersExceptAsync(
            model.SenderUserId,
            NotificationCategoryTypes.Item,
            item.ItemId);

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
        if(model is null)
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

        if(!string.IsNullOrEmpty(model.ItemName))
            if(!Equals(item.Name, model.ItemName))
                item.Name = model.ItemName;
        
        if(!Equals(item.Description, model.ItemDescription))
            item.Description = model.ItemDescription;

        if (!await _context.UpdateEntityAsync(item))
            return new FullItemResult
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                ItemDescription = item.Description,
                Errors = new[] { "Unable to update item" }
            };

        await SetItemCacheAsync(model.ItemId, item);
        await _clientNotificationService.SendUpdatedNotificationToAllUsersExceptAsync(
            model.SenderUserId,
            NotificationCategoryTypes.Item,
            item.ItemId);

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

        var usersOwningTheItem = await _mediator.Send(new GetUserIdsOwningItemQuery { ItemId = model.ItemId });

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
        
        await _mediator.Send(new RemoveItemFromUsersCommand { ItemId = model.ItemId, UserIds = usersOwningTheItem.UserIds });
        await _clientNotificationService.SendDeletedNotificationToAllUsersExceptAsync(
            model.UserId,
            NotificationCategoryTypes.Item,
            model.ItemId);
        
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

    private Task SetItemCacheAsync(string itemId, Entities.Item entity) => _cacheService.SetCacheValueAsync(CacheKeys.Item.GetItemKey(itemId), entity);

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Entities.Item>> GetItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string itemId) =>
            context.Items
                .AsNoTracking()
                .FirstOrDefault(x => x.ItemId == itemId)
        );

    #endregion Queries
}
