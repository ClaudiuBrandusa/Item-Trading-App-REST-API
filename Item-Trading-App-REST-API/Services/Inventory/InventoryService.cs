using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Item;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly DatabaseContext _context;
        private readonly IItemService _itemService;
        private readonly ICacheService _cacheService;

        public InventoryService(DatabaseContext context, IItemService itemService, ICacheService cacheService)
        {
            _context = context;
            _itemService = itemService;
            _cacheService = cacheService;
        }

        public async Task<bool> HasItem(string userId, string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId) || quantity < 1)
            {
                return false;
            }

            var amount = await GetAmountOfFreeItem(userId, itemId);

            return amount >= quantity;
        }

        public async Task<QuantifiedItemResult> AddItemAsync(string userId, string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = await _cacheService.GetCacheValueAsync<InventoryItem>(GetPrefix(userId) + itemId);

            if (item is null)
            {
                var tmp = await GetItem(userId, itemId);

                if (tmp is not null)
                {
                    item = new InventoryItem
                    {
                        Id = tmp.ItemId,
                        Quantity = tmp.Quantity
                    };
                }
            }

            var itemData = await _itemService.GetItemAsync(itemId);

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

            await _cacheService.SetCacheValueAsync(GetPrefix(userId) + itemId, new InventoryItem { Id = itemId, Quantity = quantity });

            if (updated == 0)
            {
                return new QuantifiedItemResult
                {
                    ItemId = item.Id,
                    ItemName = itemData.ItemName,
                    ItemDescription = itemData.ItemDescription,
                    Quantity = item is null ? quantity : await GetAmountOfFreeItem(userId, item.Id),
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new QuantifiedItemResult
            {
                ItemId = itemId,
                ItemName = itemData.ItemName,
                ItemDescription = itemData.ItemDescription,
                Quantity = item is null ? quantity : await GetAmountOfFreeItem(userId, item.Id),
                Success = true
            };
        }

        public async Task<QuantifiedItemResult> DropItemAsync(string userId, string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
            {
                return new QuantifiedItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = await _cacheService.GetCacheValueAsync<InventoryItem>(GetPrefix(userId) + itemId);

            if (item is null)
            {
                var tmp = await GetItem(userId, itemId);

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
                var lockedItem = await GetLockedItem(userId, itemId);
                lockedAmount = lockedItem?.Quantity ?? 0;
            }
            else
            {
                lockedAmount = await _cacheService.GetCacheValueAsync<int>(GetLockedAmountKey(userId, itemId));
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
                await _cacheService.ClearCacheKeyAsync(GetPrefix(userId) + itemId);
                await _cacheService.ClearCacheKeyAsync(GetLockedAmountKey(userId, itemId));
            }
            else
            {
                await _cacheService.SetCacheValueAsync(GetPrefix(userId) + itemId, new InventoryItem { Id = itemId, Quantity = item.Quantity });
                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedAmount);
            }

            await _context.SaveChangesAsync();

            return new QuantifiedItemResult
            {
                ItemId = itemId,
                ItemName = await _itemService.GetItemNameAsync(itemId),
                Quantity = item.Quantity == 0 ? 0 : await GetAmountOfFreeItem(userId, item.Id),
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

            int amount = await GetAmountOfFreeItem(userId, itemId);
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
                ItemName = await _itemService.GetItemNameAsync(itemId),
                ItemDescription = await _itemService.GetItemDescriptionAsync(itemId),
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

                await Parallel.ForEachAsync(tmp, async (ownedItem, cancellationToken) =>
                {
                    await _cacheService.SetCacheValueAsync(GetPrefix(userId) + ownedItem.ItemId, new InventoryItem { Id = ownedItem.ItemId, Quantity = ownedItem.Quantity });

                    var itemName = await _itemService.GetItemNameAsync(ownedItem.ItemId);

                    if (!itemName.ToLower().StartsWith(searchString)) return;

                    lock (ownedItem) 
                    {
                        ownedItems.Add(ownedItem);
                    }
                }); 

                cached = true;
            }
            else
            {
                await Parallel.ForEachAsync(inventoryItems, async (inventoryItem, cancellationToken) =>
                {
                    var cachedItem = await _cacheService.GetCacheValueAsync<Entities.Item>(CachePrefixKeys.Items + inventoryItem.Id);

                    string itemName = "";

                    if (cachedItem is not null)
                    {
                        itemName = cachedItem.Name;
                    }
                    else
                    {
                        itemName = await _itemService.GetItemNameAsync(inventoryItem.Id);
                    }

                    if (!itemName.ToLower().StartsWith(searchString)) return;

                    var tmp = new OwnedItem
                    {
                        ItemId = inventoryItem.Id,
                        UserId = userId,
                        Quantity = inventoryItem.Quantity
                    };

                    lock (tmp)
                    {
                        ownedItems.Add(tmp);
                    }
                });
            }

            if (ownedItems is null)
            {
                return new ItemsResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if(!cached)
                await Parallel.ForEachAsync(ownedItems.Select(x => new InventoryItem { Id = x.ItemId, Quantity = x.Quantity }), async (item, cancellationToken) => await _cacheService.SetCacheValueAsync(GetPrefix(userId) + item.Id, item));

            return new ItemsResult
            {
                Success = true,
                ItemsId = ownedItems.Select(oi => oi.ItemId)
            };
        }

        public async Task<LockItemResult> LockItemAsync(string userId, string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId) || quantity < 1)
            {
                return new LockItemResult
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            int amount = await GetAmountOfFreeItem(userId, itemId);

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

            return new LockItemResult
            {
                UserId = userId,
                ItemId = itemId,
                Quantity = quantity,
                Success = true
            };
        }

        public async Task<LockItemResult> UnlockItemAsync(string userId, string itemId, int quantity)
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

            var lockedItem = await GetLockedItem(userId, itemId);

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
                lockedAmount = await _cacheService.GetCacheValueAsync<int>(GetLockedAmountKey(userId, itemId));
            }
            else
            {
                var lockedItemEntity = await GetLockedItem(userId, itemId);

                lockedAmount = lockedItemEntity?.Quantity ?? 0;

                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedAmount);
            }

            var itemData = await _itemService.GetItemAsync(itemId);

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

        private async Task<OwnedItem> GetItem(string userId, string itemId) => await _context.OwnedItems.FirstOrDefaultAsync(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

        private async Task<LockedItem> GetLockedItem(string userId, string itemId) => await _context.LockedItems.FirstOrDefaultAsync(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

        private async Task<int> GetAmountOfFreeItem(string userId, string itemId)
        {
            var item = await _cacheService.GetCacheValueAsync<InventoryItem>(GetPrefix(userId) + itemId);
            
            if (item is null)
            {
                var tmp = await GetItem(userId, itemId);

                item = new InventoryItem { Id = itemId, Quantity = tmp.Quantity };

                await _cacheService.SetCacheValueAsync(GetPrefix(userId) + itemId, item);
            }

            int lockedItemQuantity = 0;

            if (!await _cacheService.ContainsKey(GetLockedAmountKey(userId, itemId)))
            {
                var tmp = await GetLockedItem(userId, itemId);

                lockedItemQuantity = tmp?.Quantity ?? 0;

                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedItemQuantity);
            }
            else
            {
                lockedItemQuantity = await _cacheService.GetCacheValueAsync<int>(GetLockedAmountKey(userId, itemId));
            }

            return item.Quantity - lockedItemQuantity;
        }

        private async Task<int> GetAmountOfLockedItem(string userId, string itemId)
        {
            int lockedAmount = 0;

            if (!await _cacheService.ContainsKey(GetLockedAmountKey(userId, itemId)))
            {
                var entity = await GetLockedItem(userId, itemId);
                
                lockedAmount = entity?.Quantity ?? 0;

                await _cacheService.SetCacheValueAsync(GetLockedAmountKey(userId, itemId), lockedAmount);
            }
            else
            {
                lockedAmount = await _cacheService.GetCacheValueAsync<int>(GetLockedAmountKey(userId, itemId));
            }

            return lockedAmount;
        }

        private async Task<LockedItem> LockItem(string userId, string itemId, int quantity)
        {
            var lockedItem = await GetLockedItem(userId, itemId);

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

        private string GetPrefix(string userId) => CachePrefixKeys.Inventory + userId + ":" + CachePrefixKeys.InventoryItems;

        private string GetLockedAmountKey(string userId, string itemId) => CachePrefixKeys.Inventory + userId + ":" + CachePrefixKeys.InventoryLockedItem + itemId;
    }
}
