using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.Inventory;
using Item_Trading_App_REST_API.Requests.Item;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory
{
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
            {
                return false;
            }

            var amount = await GetAmountOfFreeItemAsync(userId, itemId);

            return amount >= quantity;
        }

        public async Task<QuantifiedItemResult> AddItemAsync(string userId, string itemId, int quantity, bool notify = false)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = await GetInventoryItemCacheAsync(userId, itemId);

            if (item is null)
            {
                var tmp = await GetInventoryItemEntityAsync(userId, itemId);

                if (tmp is not null)
                {
                    item = new InventoryItem
                    {
                        Id = tmp.ItemId,
                        Quantity = tmp.Quantity
                    };
                }
            }

            var itemData = await _mediator.Send(new GetItemQuery { ItemId = itemId});

            if (itemData is null)
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "Item not found" }
                };
            }

            if (quantity < 0)
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "You cannot add a negative amount of an item" }
                };
            }
            else if (quantity == 0)
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "You cannot add an amount of 0 in your inventory" }
                };
            }

            if (item is null)
            {
                // then it means that we do not own items of this type

                _context.OwnedItems.Add(new OwnedItem
                {
                    ItemId = itemId,
                    UserId = userId,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
                _context.OwnedItems.Update(new OwnedItem
                {
                    UserId = userId,
                    ItemId = item.Id,
                    Quantity = item.Quantity
                });

                quantity = item.Quantity;
            }

            var updated = await _context.SaveChangesAsync();

            await _cacheService.SetCacheValueAsync(GetAmountKey(userId, itemId), new InventoryItem { Id = itemId, Quantity = quantity });
            quantity = item is null ? quantity : await GetAmountOfFreeItemAsync(userId, item.Id);

            if (updated == 0)
            {
                return new QuantifiedItemResult
                {
                    ItemId = item.Id,
                    ItemName = itemData.ItemName,
                    ItemDescription = itemData.ItemDescription,
                    Quantity = quantity,
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (notify)
                await _notificationService.SendUpdatedNotificationToUserAsync(userId, NotificationCategoryTypes.Inventory, itemId, new InventoryItemQuantityNotification
                {
                    AddAmount = true,
                    Amount = quantity
                });

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
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = await GetInventoryItemCacheAsync(userId, itemId);

            if (item is null)
            {
                var tmp = await GetInventoryItemEntityAsync(userId, itemId);

                if (tmp is null)
                {
                    return new QuantifiedItemResult
                    {
                        Errors = new[] { "Item not found" }
                    };
                }

                item = new InventoryItem
                {
                    Id = tmp.ItemId,
                    Quantity = tmp.Quantity
                };
            }

            if (quantity < 0)
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "You cannot drop a negative amount of an item" }
                };
            }
            else if (quantity == 0)
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "You cannot drop an amount of 0 from your inventory" }
                };
            }

            int freeItems = item.Quantity;
            int lockedAmount = 0;

            if (!await _cacheService.ContainsKey(GetLockedAmountKey(userId, itemId)))
            {
                var lockedItem = await GetLockedInventoryItemEntityAsync(userId, itemId);
                lockedAmount = lockedItem?.Quantity ?? 0;
            }
            else
            {
                lockedAmount = await GetLockedItemAmountCacheAsync(userId, itemId);
            }

            freeItems -= lockedAmount;
            
            if (freeItems < quantity)
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "You cannot drop more than you have" }
                };
            }

            item.Quantity -= quantity;
            
            if (item.Quantity == 0)
            {
                _context.OwnedItems.Remove(new OwnedItem { ItemId = item.Id, UserId = userId });
                await _cacheService.ClearCacheKeyAsync(GetAmountKey(userId, itemId));
                await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, itemId));
            }
            else
            {
                await _cacheService.SetCacheValueAsync(GetAmountKey(userId, itemId), new InventoryItem { Id = itemId, Quantity = item.Quantity });
                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedAmount);
            }

            await _context.SaveChangesAsync();
            quantity = item.Quantity == 0 ? 0 : await GetAmountOfFreeItemAsync(userId, item.Id);

            if (notify)
                await _notificationService.SendUpdatedNotificationToUserAsync(userId, NotificationCategoryTypes.Inventory, itemId, new InventoryItemQuantityNotification
                {
                    AddAmount = false,
                    Amount = quantity
                });

            return new QuantifiedItemResult
            {
                ItemId = itemId,
                ItemName = await _mediator.Send(new GetItemNameQuery { ItemId = itemId }),
                Quantity = quantity,
                Success = true
            };
        }

        public async Task<QuantifiedItemResult> GetItemAsync(string userId, string itemId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            int amount = await GetAmountOfFreeItemAsync(userId, itemId);
            var lockedAmount = await GetLockedAmount(userId, itemId);

            if (amount == 0 && lockedAmount.Amount == 0)
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "You do not own this item" }
                };
            }

            return new QuantifiedItemResult
            {
                ItemId = itemId,
                ItemName = await _mediator.Send(new GetItemNameQuery { ItemId = itemId }),
                ItemDescription = await _mediator.Send(new GetItemDescriptionQuery { ItemId = itemId }),
                Quantity = amount,
                Success = true
            };
        }

        public async Task<ItemsResult> ListItemsAsync(string userId, string searchString = "")
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ItemsResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var inventoryItems = (await _cacheService.ListWithPrefix<InventoryItem>(GetPrefix(userId))).Values.ToList();

            List<OwnedItem> ownedItems = new List<OwnedItem>();

            bool cached = false;
            if (!string.IsNullOrEmpty(searchString))
                searchString = searchString.ToLower();

            if (inventoryItems.Count == 0)
            {
                var tmp = _context.OwnedItems.Where(oi => Equals(oi.UserId, userId))?.ToList();

                foreach(var ownedItem in tmp)
                {
                    await _cacheService.SetCacheValueAsync(GetPrefix(userId) + ownedItem.ItemId, new InventoryItem { Id = ownedItem.ItemId, Quantity = ownedItem.Quantity });

                    var itemName = await _mediator.Send(new GetItemNameQuery { ItemId = ownedItem.ItemId });

                    if (!itemName.ToLower().StartsWith(searchString)) continue;

                    ownedItems.Add(ownedItem);
                }; 

                cached = true;
            }
            else
            {
                foreach(var inventoryItem in inventoryItems)
                {
                    string itemName = await _mediator.Send(new GetItemNameQuery { ItemId = inventoryItem.Id });

                    if (!itemName.ToLower().StartsWith(searchString)) continue;

                    var tmp = new OwnedItem
                    {
                        ItemId = inventoryItem.Id,
                        UserId = userId,
                        Quantity = inventoryItem.Quantity
                    };

                    ownedItems.Add(tmp);
                };
            }

            if (ownedItems is null)
            {
                return new ItemsResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!cached)
                ownedItems.Select(x => new InventoryItem { Id = x.ItemId, Quantity = x.Quantity }).ToList().ForEach(async (item) => await _cacheService.SetCacheValueAsync(GetPrefix(userId) + item.Id, item));
            
            return new ItemsResult
            {
                Success = true,
                ItemsId = ownedItems.Select(oi => oi.ItemId)
            };
        }

        public async Task<LockItemResult> LockItemAsync(string userId, string itemId, int quantity, bool notify = false)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId) || quantity < 1)
            {
                return new LockItemResult
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            int amount = await GetAmountOfFreeItemAsync(userId, itemId);

            if (amount < quantity)
            {
                return new LockItemResult
                {
                    Errors = new[] { "You do not own enough of this item" }
                };
            }

            var lockedItem = await LockItem(userId, itemId, quantity);

            if (lockedItem is null)
            {
                return new LockItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (notify)
                await _notificationService.SendUpdatedNotificationToUserAsync(userId, NotificationCategoryTypes.Inventory, itemId, new InventoryItemQuantityNotification
                {
                    AddAmount = true,
                    Amount = await GetAmountOfFreeItemAsync(userId, itemId),
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
            {
                return new LockItemResult
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            int amount = await GetAmountOfLockedItem(userId, itemId);

            if(quantity > amount)
            {
                return new LockItemResult
                {
                    Errors = new[] { "Cannot unlock an amount more than you have locked" }
                };
            }

            var lockedItem = await GetLockedInventoryItemEntityAsync(userId, itemId);

            if(lockedItem is null || lockedItem == default)
            {
                return new LockItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            amount -= quantity;

            if (amount == 0)
            {
                _context.LockedItems.Remove(lockedItem);
                await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, itemId));
            }
            else
            {
                lockedItem.Quantity = amount;
                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedItem.Quantity);
            }

            int removed = await _context.SaveChangesAsync();

            if(removed == 0)
            {
                return new LockItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (notify)
                await _notificationService.SendUpdatedNotificationToUserAsync(userId, NotificationCategoryTypes.Inventory, itemId, new InventoryItemQuantityNotification
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
            {
                return new LockedItemAmountResult
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            int lockedAmount = 0;

            if (await _cacheService.ContainsKey(GetLockedAmountKey(userId, itemId)))
            {
                lockedAmount = await GetLockedItemAmountCacheAsync(userId, itemId);
            }
            else
            {
                var lockedItemEntity = await GetLockedInventoryItemEntityAsync(userId, itemId);

                lockedAmount = lockedItemEntity?.Quantity ?? 0;

                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedAmount);
            }

            var itemData = await _mediator.Send(new GetItemQuery { ItemId = itemId });

            if (itemData is null)
            {
                return new LockedItemAmountResult
                {
                    Errors = new[] { "Item not found" }
                };
            }

            return new LockedItemAmountResult
            {
                ItemId = itemId,
                ItemName = itemData.ItemName,
                Amount = lockedAmount,
                Success = true
            };
        }

        public async Task<OwnedItemByUsers> GetUsersThatOwnThisItem(string itemId)
        {
            return new OwnedItemByUsers { UserIds = await ListUsersThatOwnItemAsync(itemId), ItemId = itemId };
        }

        public async Task RemoveItem(List<string> userIds, string itemId)
        {
            foreach(var userId in userIds)
            {
                await _cacheService.ClearCacheKeyAsync(GetAmountKey(userId, itemId));
                await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, itemId));
            };
            
            await _notificationService.SendDeletedNotificationToUsersAsync(userIds, NotificationCategoryTypes.Inventory, itemId);
        }

        private async Task<OwnedItem> GetInventoryItemEntityAsync(string userId, string itemId) => await _context.OwnedItems.FirstOrDefaultAsync(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

        private async Task<LockedItem> GetLockedInventoryItemEntityAsync(string userId, string itemId) => await _context.LockedItems.FirstOrDefaultAsync(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

        private async Task<int> GetAmountOfFreeItemAsync(string userId, string itemId)
        {
            var item = await GetInventoryItemCacheAsync(userId, itemId);
            
            if (item is null)
            {
                var tmp = await GetInventoryItemEntityAsync(userId, itemId);

                item = new InventoryItem { Id = itemId, Quantity = tmp.Quantity };

                await _cacheService.SetCacheValueAsync(GetAmountKey(userId, itemId), item);
            }

            int lockedItemQuantity = 0;

            if (!await _cacheService.ContainsKey(GetLockedAmountKey(userId, itemId)))
            {
                var tmp = await GetLockedInventoryItemEntityAsync(userId, itemId);

                lockedItemQuantity = tmp?.Quantity ?? 0;

                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedItemQuantity);
            }
            else
            {
                lockedItemQuantity = await GetLockedItemAmountCacheAsync(userId, itemId);
            }

            return item.Quantity - lockedItemQuantity;
        }

        private async Task<int> GetAmountOfLockedItem(string userId, string itemId)
        {
            int lockedAmount = 0;

            if (!await _cacheService.ContainsKey(GetLockedAmountKey(userId, itemId)))
            {
                var entity = await GetLockedInventoryItemEntityAsync(userId, itemId);
                
                lockedAmount = entity?.Quantity ?? 0;

                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedAmount);
            }
            else
            {
                lockedAmount = await GetLockedItemAmountCacheAsync(userId, itemId);
            }

            return lockedAmount;
        }

        private async Task<LockedItem> LockItem(string userId, string itemId, int quantity)
        {
            var lockedItem = await GetLockedInventoryItemEntityAsync(userId, itemId);

            if (lockedItem is null)
            {
                lockedItem = new LockedItem { UserId = userId, ItemId = itemId, Quantity = quantity };

                _context.LockedItems.Add(lockedItem);
            }
            else
            {
                lockedItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedItem.Quantity);

            return lockedItem;
        }

        private async Task<List<string>> ListUsersThatOwnItemAsync(string itemId) => await _context.OwnedItems.Where(x => x.ItemId == itemId).Select(x => x.UserId).ToListAsync();

        private async Task<InventoryItem> GetInventoryItemCacheAsync(string  userId, string itemId) => await _cacheService.GetCacheValueAsync<InventoryItem>(GetPrefix(userId) + itemId);

        private async Task<int> GetLockedItemAmountCacheAsync(string userId, string itemId) => await _cacheService.GetCacheValueAsync<int>(GetLockedAmountKey(userId, itemId));

        private string GetPrefix(string userId) => CachePrefixKeys.Inventory + userId + ":" + CachePrefixKeys.InventoryItems;

        private string GetAmountKey(string userId, string itemId) => GetPrefix(userId) + itemId;

        private string GetLockedAmountKey(string userId, string itemId) => CachePrefixKeys.Inventory + userId + ":" + CachePrefixKeys.InventoryLockedItem + itemId;
    }
}
