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

    public async Task<bool> HasItemAsync(string userId, string itemId, int quantity)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId) || quantity < 1)
            return false;

        var amount = await GetAmountOfFreeItemAsync(userId, itemId);

        return amount >= quantity;
    }

    public async Task<QuantifiedItemResult> AddItemAsync(string userId, string itemId, int quantity, bool notify = false)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _cacheService.GetEntityAsync(
            GetPrefix(userId) + itemId,
            async (args) =>
            {
                var entity = await GetInventoryItemEntityAsync(userId, itemId);

                if (entity is not null)
                    return new InventoryItem
                    {
                        Id = entity.ItemId,
                        Quantity = entity.Quantity
                    };

                return null;
            });

        var itemData = await _mediator.Send(new GetItemQuery { ItemId = itemId});

        if (itemData is null)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Item not found" }
            };

        if (quantity < 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot add a negative amount of an item" }
            };
        else if (quantity == 0)
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
                ItemId = itemId,
                UserId = userId,
                Quantity = quantity
            });
        }
        else
        {
            item.Quantity += quantity;

            modified = await _context.UpdateEntityAsync(new OwnedItem
            {
                UserId = userId,
                ItemId = item.Id,
                Quantity = item.Quantity
            });

            quantity = item.Quantity;
        }

        await _cacheService.SetCacheValueAsync(
            GetAmountKey(userId, itemId),
            new InventoryItem { Id = itemId, Quantity = quantity });

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                userId,
                NotificationCategoryTypes.Inventory,
                itemId,
                new InventoryItemQuantityNotification { AddAmount = true, Amount = quantity });

        return new QuantifiedItemResult
        {
            ItemId = itemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = quantity,
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> DropItemAsync(string userId, string itemId, int quantity, bool notify = false)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

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
            });

        if (quantity < 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop a negative amount of an item" }
            };
        else if (quantity == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop an amount of 0 from your inventory" }
            };

        int freeItems = item.Quantity;
        int lockedAmount = await GetAmountOfLockedItem(userId, itemId);

        freeItems -= lockedAmount;
        
        if (freeItems < quantity)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop more than you have" }
            };

        item.Quantity -= quantity;

        bool modified;

        if (item.Quantity == 0)
        {
            modified = await _context.RemoveEntityAsync(new OwnedItem
            {
                ItemId = item.Id,
                UserId = userId
            });
            await _cacheService.ClearCacheKeyAsync(GetAmountKey(userId, itemId));
            await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, itemId));
        }
        else
        {
            modified = await _context.UpdateEntityAsync(new OwnedItem
            {
                ItemId = item.Id,
                UserId = userId,
                Quantity = item.Quantity,
            });
            await _cacheService.SetCacheValueAsync(GetAmountKey(userId, itemId), new InventoryItem { Id = itemId, Quantity = item.Quantity });
            await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedAmount);
        }

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                userId,
                NotificationCategoryTypes.Inventory,
                itemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = false,
                    Amount = item.Quantity
                });

        return new QuantifiedItemResult
        {
            ItemId = itemId,
            ItemName = await _mediator.Send(new GetItemNameQuery { ItemId = itemId }),
            Quantity = item.Quantity,
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> GetItemAsync(string userId, string itemId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        int amount = await GetAmountOfFreeItemAsync(userId, itemId);
        var lockedAmount = await GetLockedAmount(userId, itemId);

        if (amount == 0 && lockedAmount.Amount == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You do not own this item" }
            };

        var itemData = await _mediator.Send(new GetItemQuery { ItemId = itemId });

        return new QuantifiedItemResult
        {
            ItemId = itemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<ItemsResult> ListItemsAsync(string userId, string searchString = "")
    {
        if (string.IsNullOrEmpty(userId))
            return new ItemsResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var inventoryItems = await _cacheService.GetEntitiesAsync(
            GetPrefix(userId),
            async (args) => await _context
                .OwnedItems
                .AsNoTracking()
                .Where(oi => Equals(oi.UserId, userId))
                .Select(x => new InventoryItem { Id = x.ItemId, Quantity = x.Quantity })
                .ToListAsync(),
            true,
            (InventoryItem item) => item.Id);

        List<string> itemIds = new();

        if (!string.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            foreach (var inventoryItem in inventoryItems)
            {
                string itemName = await _mediator.Send(new GetItemNameQuery { ItemId = inventoryItem.Id });

                if (!itemName.ToLower().StartsWith(searchString)) continue;

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

    public async Task<LockItemResult> LockItemAsync(string userId, string itemId, int quantity, bool notify = false)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId) || quantity < 1)
            return new LockItemResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int amount = await GetAmountOfFreeItemAsync(userId, itemId);

        if (amount < quantity)
            return new LockItemResult
            {
                Errors = new[] { "You do not own enough of this item" }
            };

        if (!await LockItem(userId, itemId, quantity))
            return new LockItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                userId,
                NotificationCategoryTypes.Inventory,
                itemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = true,
                    Amount = amount
                });

        return new LockItemResult
        {
            UserId = userId,
            ItemId = itemId,
            Quantity = quantity,
            Success = true
        };
    }

    public async Task<LockItemResult> UnlockItemAsync(string userId, string itemId, int quantity, bool notify = false)
    {
        if(string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId) || quantity < 1)
            return new LockItemResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int amount = await GetAmountOfLockedItem(userId, itemId);

        if(amount == 0 || quantity > amount)
            return new LockItemResult
            {
                Errors = new[] { "Cannot unlock more than you have locked" }
            };

        amount -= quantity;
        bool modified;
        var lockedItem = new LockedItem { UserId = userId, ItemId = itemId, Quantity = amount };

        if (amount == 0)
        {
            modified = await _context.RemoveEntityAsync(lockedItem);
            await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, itemId));
        }
        else
        {
            lockedItem.Quantity = amount;
            modified = await _context.UpdateEntityAsync(lockedItem);
            await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedItem.Quantity);
        }

        if(!modified)
            return new LockItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (notify)
            await _notificationService.SendUpdatedNotificationToUserAsync(
                userId,
                NotificationCategoryTypes.Inventory,
                itemId,
                new InventoryItemQuantityNotification
                {
                    AddAmount = true,
                    Amount = await GetAmountOfFreeItemAsync(userId, itemId),
                });

        return new LockItemResult
        {
            ItemId = itemId,
            UserId = userId,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<LockedItemAmountResult> GetLockedAmount(string userId, string itemId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            return new LockedItemAmountResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int lockedAmount = await GetAmountOfLockedItem(userId, itemId);

        var itemName = await _mediator.Send(new GetItemNameQuery { ItemId = itemId });

        if (string.IsNullOrEmpty(itemName))
            return new LockedItemAmountResult
            {
                Errors = new[] { "Item not found" }
            };

        return new LockedItemAmountResult
        {
            ItemId = itemId,
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

    public async Task RemoveItemAsync(List<string> userIds, string itemId)
    {
        foreach(var userId in userIds)
        {
            await _cacheService.ClearCacheKeyAsync(GetAmountKey(userId, itemId));
            await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, itemId));
        };
        
        await _notificationService.SendDeletedNotificationToUsersAsync(userIds, NotificationCategoryTypes.Inventory, itemId);
    }

    private Task<OwnedItem> GetInventoryItemEntityAsync(string userId, string itemId) => 
        _context.OwnedItems
            .AsNoTracking()
            .FirstOrDefaultAsync(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

    private Task<LockedItem> GetLockedInventoryItemEntityAsync(string userId, string itemId) => 
        _context.LockedItems
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

        return item.Quantity - lockedItemQuantity;
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
