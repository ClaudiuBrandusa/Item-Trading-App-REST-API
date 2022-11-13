using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Item;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly DatabaseContext _context;
        private readonly IItemService _itemService;

        public InventoryService(DatabaseContext context, IItemService itemService)
        {
            _context = context;
            _itemService = itemService;
        }

        public bool HasItem(string userId, string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId) || quantity < 1)
            {
                return false;
            }

            var amount = GetAmountOfFreeItem(userId, itemId);

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

            var item = GetItem(userId, itemId);

            var itemData = await _itemService.GetItemAsync(itemId);

            if (itemData == null)
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

            if (item == null)
            {
                // then it means that we do not own items of this type

                item = new OwnedItem { ItemId = itemId, UserId = userId, Quantity = quantity };

                _context.OwnedItems.Add(item);
            }
            else
            {
                item.Quantity += quantity;
            }

            var updated = await _context.SaveChangesAsync();

            if (updated == 0)
            {
                return new QuantifiedItemResult
                {
                    ItemId = item.ItemId,
                    ItemName = itemData.ItemName,
                    ItemDescription = itemData.ItemDescription,
                    Quantity = GetAmountOfFreeItem(userId, item.ItemId),
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new QuantifiedItemResult
            {
                ItemId = itemId,
                ItemName = itemData.ItemName,
                ItemDescription = itemData.ItemDescription,
                Quantity = GetAmountOfFreeItem(userId, item.ItemId),
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

            var item = GetItem(userId, itemId);

            if (item == null)
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

            var lockedItem = GetLockedItem(userId, itemId);

            if (lockedItem != null)
            {
                int freeItems = item.Quantity - lockedItem.Quantity;
                if (freeItems < quantity)
                {
                    return new QuantifiedItemResult
                    {
                        Errors = new[] { "You cannot drop more than you have" }
                    };
                }
            }

            item.Quantity -= quantity;

            if (item.Quantity == 0)
            {
                _context.OwnedItems.Remove(item);
            }

            await _context.SaveChangesAsync();

            return new QuantifiedItemResult
            {
                ItemId = itemId,
                ItemName = await _itemService.GetItemNameAsync(itemId),
                Quantity = GetAmountOfFreeItem(userId, item.ItemId),
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

            int amount = GetAmountOfFreeItem(userId, itemId);

            if (amount == 0)
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

            var ownedItems = _context.OwnedItems.Where(oi => Equals(oi.UserId, userId))?.ToList();

            if (!string.IsNullOrEmpty(searchString))
                ownedItems = (from item in _context.OwnedItems
                             where item.Item.Name.StartsWith(searchString)
                             select item).ToList();

            if (ownedItems == null)
            {
                return new ItemsResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

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

            int amount = GetAmountOfFreeItem(userId, itemId);

            if (amount < quantity)
            {
                return new LockItemResult
                {
                    Errors = new[] { "You do not own enough of this item" }
                };
            }

            var lockedItem = await LockItem(userId, itemId, quantity);

            if (lockedItem == null)
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

            int amount = GetAmountOfLockedItem(userId, itemId);

            if(quantity > amount)
            {
                return new LockItemResult
                {
                    Errors = new[] { "Cannot unlock an amount more than you have locked" }
                };
            }

            var lockedItem = GetLockedItem(userId, itemId);

            if(lockedItem == null || lockedItem == default)
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
            }
            else
            {
                lockedItem.Quantity = amount;
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

        private OwnedItem GetItem(string userId, string itemId) => _context.OwnedItems.FirstOrDefault(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

        private LockedItem GetLockedItem(string userId, string itemId) => _context.LockedItems.FirstOrDefault(oi => Equals(oi.UserId, userId) && Equals(oi.ItemId, itemId));

        private int GetAmountOfFreeItem(string userId, string itemId)
        {
            var item = GetItem(userId, itemId);

            var lockedItem = GetLockedItem(userId, itemId);

            if (item == null || item == default)
            {
                return 0;
            }

            if (lockedItem == null || lockedItem == default)
            {
                return item.Quantity;
            }

            return item.Quantity - lockedItem.Quantity;
        }

        private int GetAmountOfLockedItem(string userId, string itemId)
        {
            var item = GetLockedItem(userId, itemId);

            if(item == null || item == default)
            {
                return 0;
            }

            return item.Quantity;
        }

        private async Task<LockedItem> LockItem(string userId, string itemId, int quantity)
        {
            var lockedItem = GetLockedItem(userId, itemId);

            if (lockedItem == null)
            {
                lockedItem = new LockedItem { UserId = userId, ItemId = itemId, Quantity = quantity };

                _context.LockedItems.Add(lockedItem);
            }
            else
            {
                lockedItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            return lockedItem;
        }
    }
}
