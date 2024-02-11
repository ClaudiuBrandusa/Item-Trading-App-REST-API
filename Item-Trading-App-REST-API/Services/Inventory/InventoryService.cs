using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Notification;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Events.Inventory;

namespace Item_Trading_App_REST_API.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly DatabaseContext _context;
    private readonly IClientNotificationService _clientNotificationService;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public InventoryService(DatabaseContext context, IClientNotificationService clientNotificationService, ICacheService cacheService, IMediator mediator, IMapper mapper)
    {
        _context = context;
        _clientNotificationService = clientNotificationService;
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<bool> HasItemAsync(HasItemQuantityQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return false;

        var amount = await GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        return amount >= model.Quantity;
    }

    public async Task<QuantifiedItemResult> AddItemAsync(AddInventoryItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (model.Quantity < 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot add a negative amount of an item" }
            };
        else if (model.Quantity == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot add an amount of 0 in your inventory" }
            };

        var itemData = await _mediator.Send(new GetItemQuery { ItemId = model.ItemId });

        if (itemData is null)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Item not found" }
            };

        var item = await _cacheService.GetEntityAsync(
            CacheKeys.Inventory.GetAmountKey(model.UserId, model.ItemId),
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(model.UserId, model.ItemId);

                if (entity is not null)
                    return _mapper.AdaptToType<OwnedItem, InventoryItem>(entity);

                return null;
            });

        bool modified;

        if (item is null)
        {
            // then it means that we do not own items of this type
            var entity = _mapper.AdaptToType<AddInventoryItemCommand, OwnedItem>(model);

            modified = await _context.AddEntityAsync(entity);
        }
        else
        {
            model.Quantity += item.Quantity;

            var entity = _mapper.AdaptToType<AddInventoryItemCommand, OwnedItem>(model);

            modified = await _context.UpdateEntityAsync(entity);
        }

        await _cacheService.SetCacheValueAsync(
            CacheKeys.Inventory.GetAmountKey(model.UserId, model.ItemId),
            _mapper.AdaptToType<AddInventoryItemCommand, InventoryItem>(model));

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        model.Quantity -= await GetAmountOfLockedItem(model.UserId, model.ItemId);

        var eventNotification = _mapper.AdaptToType<AddInventoryItemCommand, InventoryItemAddedEvent>(model);

        await _mediator.Publish(eventNotification);

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = model.Quantity,
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> DropItemAsync(DropInventoryItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (model.Quantity < 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop a negative amount of an item" }
            };
        else if (model.Quantity == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop an amount of 0 from your inventory" }
            };

        var item = await _cacheService.GetEntityAsync(
            CacheKeys.Inventory.GetAmountKey(model.UserId, model.ItemId),
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(model.UserId, model.ItemId);

                if (entity is not null)
                {
                    return _mapper.AdaptToType<OwnedItem, InventoryItem>(entity);
                }

                return null;
            });

        int freeItems = item.Quantity;
        int lockedAmount = await GetAmountOfLockedItem(model.UserId, model.ItemId);

        freeItems -= lockedAmount;
        
        if (freeItems < model.Quantity)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop more than you have" }
            };

        model.Quantity = freeItems - model.Quantity;

        bool modified;
        var entity = _mapper.AdaptToType<DropInventoryItemCommand, OwnedItem>(model);

        if (model.Quantity == 0)
        {
            modified = await _context.RemoveEntityAsync(entity);
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetAmountKey(model.UserId, model.ItemId));
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(model.UserId, model.ItemId));
        }
        else
        {
            modified = await _context.UpdateEntityAsync(entity);
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetAmountKey(model.UserId, model.ItemId), item);
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(model.UserId, model.ItemId), lockedAmount);
        }

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var eventNotification = _mapper.AdaptToType<DropInventoryItemCommand, InventoryItemDroppedEvent>(model);

        await _mediator.Publish(eventNotification);

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = await _mediator.Send(new GetItemNameQuery { ItemId = model.ItemId }),
            Quantity = model.Quantity,
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> GetItemAsync(GetInventoryItemQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        int amount = await GetAmountOfFreeItemAsync(model.UserId, model.ItemId);
        var lockedAmount = await GetLockedAmountAsync(_mapper.AdaptToType<GetInventoryItemQuery, GetInventoryItemLockedAmountQuery>(model));

        if (amount == 0 && lockedAmount.Amount == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You do not own this item" }
            };

        var itemData = await _mediator.Send(new GetItemQuery { ItemId = model.ItemId });

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<ItemsResult> ListItemsAsync(ListInventoryItemsQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new ItemsResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var inventoryItems = await _cacheService.GetEntitiesAsync(
            CacheKeys.Inventory.GetUserInventoryKey(model.UserId),
            async (args) => await _context
                .OwnedItems
                .AsNoTracking()
                .Where(oi => Equals(oi.UserId, model.UserId))
                .Select(x => _mapper.AdaptToType<OwnedItem, InventoryItem>(x))
                .ToArrayAsync(),
            true,
            (InventoryItem item) => item.Id);

        List<string> itemIds = new();

        if (!string.IsNullOrEmpty(model.SearchString))
        {
            foreach (var inventoryItem in inventoryItems)
            {
                string itemName = await _mediator.Send(new GetItemNameQuery { ItemId = inventoryItem.Id });
                
                if (!itemName.StartsWith(model.SearchString, StringComparison.OrdinalIgnoreCase)) continue;

                itemIds.Add(inventoryItem.Id);
            };
        }
        else
        {
            itemIds = inventoryItems.Select(x => x.Id).ToList();
        }

        return new ItemsResult
        {
            Success = true,
            ItemsId = itemIds
        };
    }

    public async Task<LockItemResult> LockItemAsync(LockItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return new LockItemResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int amount = await GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        if (amount < model.Quantity)
            return new LockItemResult
            {
                Errors = new[] { "You do not own enough of this item" }
            };

        if (!await LockItem(model.UserId, model.ItemId, model.Quantity))
            return new LockItemResult
            {
                Errors = new[] { "Something went wrong" }
            };
        else
            amount -= model.Quantity;

        var notificationEvent = _mapper.AdaptToType<LockItemCommand, InventoryItemLockedEvent> (model, (nameof(InventoryItemLockedEvent.Quantity), amount));

        await _mediator.Publish(notificationEvent);

        return new LockItemResult
        {
            UserId = model.UserId,
            ItemId = model.ItemId,
            Quantity = model.Quantity,
            Success = true
        };
    }

    public async Task<LockItemResult> UnlockItemAsync(UnlockItemCommand model)
    {
        if(string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return new LockItemResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int amount = await GetAmountOfLockedItem(model.UserId, model.ItemId);

        if(amount == 0 || model.Quantity > amount)
            return new LockItemResult
            {
                Errors = new[] { "Cannot unlock more than you have locked" }
            };

        amount -= model.Quantity;
        bool modified;
        var lockedItem = new LockedItem { UserId = model.UserId, ItemId = model.ItemId, Quantity = amount };

        if (amount == 0)
        {
            modified = await _context.RemoveEntityAsync(lockedItem);
            await _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(model.UserId, model.ItemId));
        }
        else
        {
            lockedItem.Quantity = amount;
            modified = await _context.UpdateEntityAsync(lockedItem);
            await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(model.UserId, model.ItemId), lockedItem.Quantity);
        }

        if(!modified)
            return new LockItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var eventNotification = _mapper.AdaptToType<UnlockItemCommand, InventoryItemUnlockedEvent>(model, (nameof(InventoryItemUnlockedEvent.Quantity), await GetAmountOfFreeItemAsync(model.UserId, model.ItemId)));

        await _mediator.Publish(eventNotification);

        return new LockItemResult
        {
            ItemId = model.ItemId,
            UserId = model.UserId,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<LockedItemAmountResult> GetLockedAmountAsync(GetInventoryItemLockedAmountQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new LockedItemAmountResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int lockedAmount = await GetAmountOfLockedItem(model.UserId, model.ItemId);

        var itemName = await _mediator.Send(new GetItemNameQuery { ItemId = model.ItemId });

        if (string.IsNullOrEmpty(itemName))
            return new LockedItemAmountResult
            {
                Errors = new[] { "Item not found" }
            };

        return new LockedItemAmountResult
        {
            ItemId = model.ItemId,
            ItemName = itemName,
            Amount = lockedAmount,
            Success = true
        };
    }

    public async Task<UsersOwningItem> GetUsersOwningThisItemAsync(GetUserIdsOwningItemQuery model) =>
        new UsersOwningItem
        {
            UserIds = await ListUsersThatOwnItemAsync(model.ItemId),
            ItemId = model.ItemId
        };

    public async Task RemoveItemCacheAsync(RemoveItemFromUsersCommand model)
    {
        var tasks = new Task[model.UserIds.Length];

        for (int i = 0; i < model.UserIds.Length; i++)
        {
            string userId = model.UserIds[i];

            tasks[i] = Task.WhenAll(
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetAmountKey(userId, model.ItemId)),
                _cacheService.ClearCacheKeyAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, model.ItemId))
            );
        };

        await Task.WhenAll(tasks);
        await _clientNotificationService.SendDeletedNotificationToUsersAsync(model.UserIds, NotificationCategoryTypes.Inventory, model.ItemId);
    }

    private async Task<bool> LockItem(string userId, string itemId, int quantity)
    {
        bool storedInDb = await GetLockedInventoryItemEntityAsync(userId, itemId) != default;
        bool modified;

        if (!storedInDb)
        {
            modified = await _context.AddEntityAsync(new LockedItem { UserId = userId, ItemId = itemId, Quantity = quantity });
        }
        else
        {
            int lockedAmount = await GetAmountOfLockedItem(userId, itemId);

            quantity += lockedAmount;

            modified = await _context.UpdateEntityAsync(new LockedItem { UserId = userId, ItemId = itemId, Quantity = quantity });
        }

        await _cacheService.SetCacheValueAsync(CacheKeys.Inventory.GetLockedAmountKey(userId, itemId), quantity);

        return modified;
    }

    private Task<OwnedItem> GetInventoryItemEntityAsync(string userId, string itemId) =>
        GetInventoryItemQuery(_context, userId, itemId);

    private Task<LockedItem> GetLockedInventoryItemEntityAsync(string userId, string itemId) => 
        GetLockedInventoryItemQuery(_context, userId, itemId);

    private async Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
    {
        var item = await _cacheService.GetEntityAsync(
            CacheKeys.Inventory.GetAmountKey(userId, itemId),
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(userId, itemId);
                if (entity is not null)
                {
                    return _mapper.AdaptToType<OwnedItem, InventoryItem>(entity);
                }

                return null;
            },
            true);

        int lockedItemQuantity = await GetAmountOfLockedItem(userId, itemId);

        if (item is null) return 0;

        return item!.Quantity - lockedItemQuantity;
    }

    private Task<int> GetAmountOfLockedItem(string userId, string itemId)
    {
        return _cacheService.GetEntityAsync(
            CacheKeys.Inventory.GetLockedAmountKey(userId, itemId),
            async (args) =>
            {
                var tmp = await GetLockedInventoryItemEntityAsync(userId, itemId);

                return tmp?.Quantity ?? 0;
            },
            true);
    }

    private Task<string[]> ListUsersThatOwnItemAsync(string itemId) =>
        _context.OwnedItems
            .AsNoTracking()
            .Where(x => x.ItemId == itemId).Select(x => x.UserId)
            .ToArrayAsync();

    #region Queries

    private static readonly Func<DatabaseContext, string, string, Task<OwnedItem>> GetInventoryItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.OwnedItems
                .AsNoTracking()
                .FirstOrDefault(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId))
        );

    private static readonly Func<DatabaseContext, string, string, Task<LockedItem>> GetLockedInventoryItemQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string userId, string itemId) =>
            context.LockedItems
                .AsNoTracking()
                .FirstOrDefault(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId))
        );

    #endregion Queries
}
