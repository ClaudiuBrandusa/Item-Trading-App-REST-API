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
using Item_Trading_App_REST_API.Requests.Item;
using Item_Trading_App_REST_API.Requests.Inventory;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Requests.Trade;

namespace Item_Trading_App_REST_API.Services.Item;

public class ItemService : IItemService
{
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly INotificationService _notificationService;
    private readonly IMediator _mediator;

    public ItemService(DatabaseContext context, ICacheService cacheService, INotificationService notificationService, IMediator mediator)
    {
        _context = context;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _mediator = mediator;
    }

    public async Task<FullItemResult> CreateItemAsync(CreateItem model)
    {
        if(model is null)
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = new Entities.Item
        {
            ItemId = Guid.NewGuid().ToString(),
            Name = model.ItemName,
            Description = model.ItemDescription
        };

        if (!await _context.AddEntityAsync(item))
            return new FullItemResult
            {
                Errors = new[] { "Unable to add this item" }
            };

        await SetItemCacheAsync(item.ItemId, item);
        await _notificationService.SendCreatedNotificationToAllUsersExceptAsync(
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

    public async Task<FullItemResult> UpdateItemAsync(UpdateItem model)
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
        await _notificationService.SendUpdatedNotificationToAllUsersExceptAsync(
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

    public async Task<DeleteItemResult> DeleteItemAsync(string itemId, string senderUserId)
    {
        if(string.IsNullOrEmpty(itemId))
            return new DeleteItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var isUsedInATrade = await _mediator.Send(new ItemUsedInTradeQuery { ItemId = itemId });

        if (isUsedInATrade)
            return new DeleteItemResult
            {
                Errors = new[] { "Unable to delete an item that is used in a trade" }
            };

        var usersOwningTheItem = await _mediator.Send(new GetUserIdsOwningItem { ItemId = itemId });

        string cacheKey = CacheKeys.Item.GetItemKey(itemId);

        var item = await _cacheService.GetEntityAsync(
            cacheKey,
            (args) => GetItemEntityAsync(itemId));

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
        
        await _mediator.Send(new ItemDeleted { ItemId = itemId, UserIds = usersOwningTheItem });
        await _notificationService.SendDeletedNotificationToAllUsersExceptAsync(
            senderUserId,
            NotificationCategoryTypes.Item,
            itemId);
        
        return new DeleteItemResult
        {
            ItemId = itemId,
            ItemName = item.Name,
            Success = true
        };
    }

    public async Task<FullItemResult> GetItemAsync(string itemId)
    {
        if(string.IsNullOrEmpty(itemId))
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(itemId),
            (args) => GetItemEntityAsync(itemId),
            true);

        if (item is null)
            return new FullItemResult
            {
                ItemId = itemId,
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

    public async Task<ItemsResult> ListItemsAsync(string searchString = "")
    {
        var items = await _cacheService.GetEntitiesAsync(
            CacheKeys.Item.GetItemsKey(),
            (args) => _context.Items.AsNoTracking().ToListAsync(),
            true,
            (Entities.Item x) => x.ItemId);

        if (!string.IsNullOrEmpty(searchString))
            items = items.Where(x => x.Name.ToLower().StartsWith(searchString.ToLower())).ToList();

        return new ItemsResult
        {
            ItemsId = items.Select(i => i.ItemId),
            Success = true
        };
    }

    public async Task<string> GetItemNameAsync(string itemId)
    {
        var entity = await _cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(itemId),
            (args) => GetItemEntityAsync(itemId),
            true);

        return entity?.Name ?? "";
    }

    public async Task<string> GetItemDescriptionAsync(string itemId)
    {
        var entity = await _cacheService.GetEntityAsync(
            CacheKeys.Item.GetItemKey(itemId),
            (args) => GetItemEntityAsync(itemId),
            true);

        return entity?.Description ?? "";
    }

    private Task<Entities.Item> GetItemEntityAsync(string itemId)
    {
        return _context.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ItemId == itemId);
    }

    private Task SetItemCacheAsync(string itemId, Entities.Item entity) => _cacheService.SetCacheValueAsync(CacheKeys.Item.GetItemKey(itemId), entity);
}
