using Domain.Common.Wrappers;
using Domain.DomainEvents.Inventories;
using Domain.Entities.Inventories;
using Domain.Primitives;

namespace Domain.Aggregates.Inventories;

public class Inventory : AggregateRoot
{
    private readonly DictionaryCollectionWrapper<string, InventoryItem> _ownedItems = new((InventoryItem inventoryItem) => inventoryItem.ItemId);

    public IReadOnlyCollection<InventoryItem> OwnedItems => _ownedItems;

    public string UserId { get; private set; }

    public Inventory(string userId)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentException($"{nameof(userId)} is null or empty.");

        UserId = userId;
    }

    public void AddItem(string itemId, int quantity, bool notify = false)
    {
        if (string.IsNullOrEmpty(itemId))
            throw new ArgumentException($"Invalid value for {nameof(itemId)}.");

        if (quantity <= 0)
            throw new ArgumentException("Invalid value for the item quantity.");

        if (_ownedItems.TryGetValue(itemId, out var existing))
        {
            existing.Increase(quantity);
        }
        else
        {
            _ownedItems.Add(new InventoryItem(UserId, itemId, quantity));
        }

        RaiseDomainEvent(new InventoryItemAddedDomainEvent(UserId, itemId, quantity, notify));
    }

    public void AddItem(InventoryItem inventoryItem, bool notify = false)
    {
        if (_ownedItems.TryGetValue(inventoryItem.ItemId, out var existing))
        {
            existing.Increase(inventoryItem.Quantity);
        }
        else
        {
            _ownedItems.Add(new InventoryItem(UserId, inventoryItem.ItemId, inventoryItem.Quantity));
        }

        RaiseDomainEvent(new InventoryItemAddedDomainEvent(UserId, inventoryItem.ItemId, inventoryItem.Quantity, notify));
    }

    public void DropItem(string itemId, int quantity)
    {
        if (string.IsNullOrEmpty(itemId))
            throw new ArgumentException($"Invalid value for {nameof(itemId)}.");

        if (quantity <= 0)
            throw new ArgumentException("Invalid value for the item quantity.");

        if (!_ownedItems.TryGetValue(itemId, out var existing))
            throw new ArgumentException("You cannot drop an item that you don't have in your inventory.");

        if (existing.FreeAmount == quantity)
        {
            if (existing.LockedAmount == 0)
            {
                _ownedItems.Remove(itemId);
            }
            else
                throw new ArgumentException("You cannot drop from your inventory more than you already have free.");
        }
        else if (existing.FreeAmount < quantity)
        {
            throw new ArgumentException("You cannot drop from your inventory more than you already have.");
        }
        else
        {
            _ownedItems[itemId].Drop(quantity);
        }

        RaiseDomainEvent(new InventoryItemDroppedDomainEvent(UserId, itemId, quantity, true));
    }

    public bool LockItem(string itemId, int lockedAmount)
    {
        var inventoryItem = GetItem(itemId);

        if (inventoryItem is null)
            throw new ArgumentException($"Unable to lock item. Inventory doesn't have an item with id {itemId}.");

        inventoryItem.Lock(lockedAmount);

        RaiseDomainEvent(new InventoryItemLockedDomainEvent(UserId, itemId, lockedAmount, true));

        return true;
    }

    public bool UnlockItem(string itemId, int unlockedAmount)
    {
        var inventoryItem = GetItem(itemId);

        if (inventoryItem is null)
            throw new ArgumentException($"Unable to lock item. Inventory doesn't have an item with id {itemId}.");

        inventoryItem.Unlock(unlockedAmount);

        RaiseDomainEvent(new InventoryItemUnlockedDomainEvent(UserId, itemId, unlockedAmount, true));

        return true;
    }

    public InventoryItem? GetItem(string itemId) => _ownedItems.ContainsKey(itemId) ? _ownedItems[itemId] : null;

    public int GetItemQuantity(string itemId) => _ownedItems.ContainsKey(itemId) ? _ownedItems[itemId].Quantity : 0;

    public int GetLockedItemAmount(string itemId) => _ownedItems.ContainsKey(itemId) ? _ownedItems[itemId].LockedAmount : 0;

    public int GetItemFreeAmount(string itemId) => _ownedItems.ContainsKey(itemId) ? _ownedItems[itemId].FreeAmount : 0;

    public IEnumerable<string> ItemIds => OwnedItems.Select(x => x.ItemId);

    protected override object GetId() => UserId;

    protected override bool Compare(object obj)
    {
        if (obj is not Inventory entity) return false;

        return entity.UserId == UserId;
    }
}
