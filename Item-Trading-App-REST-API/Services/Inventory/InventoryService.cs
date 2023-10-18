using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.Item;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly DatabaseContext _context;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;

    public InventoryService(DatabaseContext context, INotificationService notificationService, ICacheService cacheService, IMediator mediator)
    {
        _context = context;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _mediator = mediator;
    }

    public async Task<bool> HasItemAsync(HasItem model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return false;

        var amount = await GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        return amount >= model.Quantity;
    }

    public async Task<QuantifiedItemResult> AddItemAsync(AddItem model, bool notify = false)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _cacheService.GetEntityAsync(
            GetPrefix(model.UserId) + model.ItemId,
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(model.UserId, model.ItemId);

                if (entity is not null)
                    return new InventoryItem
                    {
                        Id = entity.ItemId,
                        Quantity = entity.Quantity
                    };

                return null;
            });

        var itemData = await _mediator.Send(new GetItemQuery { ItemId = model.ItemId });

        if (itemData is null)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Item not found" }
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

        bool modified;

        if (item is null)
        {
            // then it means that we do not own items of this type

            modified = await _context.AddEntityAsync(new OwnedItem
            {
                ItemId = model.ItemId,
                UserId = model.UserId,
                Quantity = model.Quantity
            });
        }
        else
        {
            item.Quantity += model.Quantity;

            modified = await _context.UpdateEntityAsync(new OwnedItem
            {
                UserId = model.UserId,
                ItemId = item.Id,
                Quantity = item.Quantity
            });

            model.Quantity = item.Quantity;
        }

        await _cacheService.SetCacheValueAsync(
            GetAmountKey(model.UserId, model.ItemId),
            new InventoryItem { Id = model.ItemId, Quantity = model.Quantity });

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                model.UserId,
                NotificationCategoryTypes.Inventory,
                model.ItemId,
                new InventoryItemQuantityNotification { AddAmount = true, Amount = model.Quantity });

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = model.Quantity,
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> DropItemAsync(DropItem model, bool notify = false)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _cacheService.GetEntityAsync(
            GetPrefix(model.UserId) + model.ItemId,
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(model.UserId, model.ItemId);

                if (entity is not null)
                {
                    return new InventoryItem
                    {
                        Id = entity.ItemId,
                        Quantity = entity.Quantity
                    };
                }

                return null;
            });

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

        int freeItems = item.Quantity;
        int lockedAmount = await GetAmountOfLockedItem(model.UserId, model.ItemId);

        freeItems -= lockedAmount;
        
        if (freeItems < model.Quantity)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop more than you have" }
            };

        item.Quantity -= model.Quantity;

        bool modified;

        if (item.Quantity == 0)
        {
            modified = await _context.RemoveEntityAsync(new OwnedItem
            {
                ItemId = item.Id,
                UserId = model.UserId
            });
            await _cacheService.ClearCacheKeyAsync(GetAmountKey(model.UserId, model.ItemId));
            await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(model.UserId, model.ItemId));
        }
        else
        {
            modified = await _context.UpdateEntityAsync(new OwnedItem
            {
                ItemId = item.Id,
                UserId = model.UserId,
                Quantity = item.Quantity,
            });
            await _cacheService.SetCacheValueAsync(GetAmountKey(model.UserId, model.ItemId), new InventoryItem { Id = model.ItemId, Quantity = item.Quantity });
            await _cacheService.SetCacheValueAsync(GetLockedAmountKey(model.UserId, model.ItemId), lockedAmount);
        }

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                model.UserId,
                NotificationCategoryTypes.Inventory,
                model.ItemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = false,
                    Amount = item.Quantity
                });

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = await _mediator.Send(new GetItemNameQuery { ItemId = model.ItemId }),
            Quantity = item.Quantity,
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> GetItemAsync(GetUsersItem model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        int amount = await GetAmountOfFreeItemAsync(model.UserId, model.ItemId);
        var lockedAmount = await GetLockedAmount(model);

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

    public async Task<ItemsResult> ListItemsAsync(ListItems model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new ItemsResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var inventoryItems = await _cacheService.GetEntitiesAsync(
            GetPrefix(model.UserId),
            async (args) => await _context
                .OwnedItems
                .AsNoTracking()
                .Where(oi => Equals(oi.UserId, model.UserId))
                .Select(x => new InventoryItem { Id = x.ItemId, Quantity = x.Quantity })
                .ToListAsync(),
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

    public async Task<LockItemResult> LockItemAsync(LockInventoryItem model, bool notify = false)
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

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                model.UserId,
                NotificationCategoryTypes.Inventory,
                model.ItemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = true,
                    Amount = amount
                });

        return new LockItemResult
        {
            UserId = model.UserId,
            ItemId = model.ItemId,
            Quantity = model.Quantity,
            Success = true
        };
    }

    public async Task<LockItemResult> UnlockItemAsync(LockInventoryItem model, bool notify = false)
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
            await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(model.UserId, model.ItemId));
        }
        else
        {
            lockedItem.Quantity = amount;
            modified = await _context.UpdateEntityAsync(lockedItem);
            await _cacheService.SetCacheValueAsync(GetLockedAmountKey(model.UserId, model.ItemId), lockedItem.Quantity);
        }

        if(!modified)
            return new LockItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                model.UserId,
                NotificationCategoryTypes.Inventory,
                model.ItemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = true,
                    Amount = await GetAmountOfFreeItemAsync(model.UserId, model.ItemId),
                });

        return new LockItemResult
        {
            ItemId = model.ItemId,
            UserId = model.UserId,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<LockedItemAmountResult> GetLockedAmount(GetUsersItem model)
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

    public async Task<OwnedItemByUsers> GetUsersThatOwnThisItem(string itemId) =>
        new OwnedItemByUsers
        {
            UserIds = await ListUsersThatOwnItemAsync(itemId),
            ItemId = itemId
        };

    public async Task RemoveItemCacheAsync(RemoveItemFromUsers model)
    {
        foreach(var userId in model.UserIds)
        {
            await _cacheService.ClearCacheKeyAsync(GetAmountKey(userId, model.ItemId));
            await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, model.ItemId));
        };
        
        await _notificationService.SendDeletedNotificationToUsersAsync(model.UserIds, NotificationCategoryTypes.Inventory, model.ItemId);
    }

    private Task<OwnedItem> GetInventoryItemEntityAsync(string userId, string itemId) => 
        _context.OwnedItems
            .AsNoTracking()
            .FirstOrDefaultAsync(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

    private Task<LockedItem> GetLockedInventoryItemEntityAsync(string userId, string itemId) => 
        _context.LockedItems
            .AsNoTracking()
            .FirstOrDefaultAsync(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

    private async Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
    {
        var item = await _cacheService.GetEntityAsync(
            GetPrefix(userId) + itemId,
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(userId, itemId);
                if (entity is not null)
                {
                    return new InventoryItem
                    {
                        Id = entity.ItemId,
                        Quantity = entity.Quantity
                    };
                }

                return null;
            },
            true);

        int lockedItemQuantity = await GetAmountOfLockedItem(userId, itemId);

        return item?.Quantity ?? 0 - lockedItemQuantity;
    }

    private Task<int> GetAmountOfLockedItem(string userId, string itemId)
    {
        return _cacheService.GetEntityAsync(
            GetLockedAmountKey(userId, itemId),
            async (args) =>
            {
                var tmp = await GetLockedInventoryItemEntityAsync(userId, itemId);

                return tmp?.Quantity ?? 0;
            },
            true);
    }

    private async Task<bool> LockItem(string userId, string itemId, int quantity)
    {
        bool storedInDb = _context.LockedItems
            .AsNoTracking()
            .FirstOrDefault(x => x.ItemId.Equals(itemId) && x.UserId.Equals(userId)) != default;
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

        await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), quantity);

        return modified;
    }

    private Task<List<string>> ListUsersThatOwnItemAsync(string itemId) =>
        _context.OwnedItems
            .AsNoTracking()
            .Where(x => x.ItemId == itemId).Select(x => x.UserId)
            .ToListAsync();

    private static string GetPrefix(string userId) => CachePrefixKeys.Inventory + userId + ":" + CachePrefixKeys.InventoryItems;

    private static string GetAmountKey(string userId, string itemId) => GetPrefix(userId) + itemId;

    private static string GetLockedAmountKey(string userId, string itemId) => CachePrefixKeys.Inventory + userId + ":" + CachePrefixKeys.InventoryLockedItem + itemId;
}
