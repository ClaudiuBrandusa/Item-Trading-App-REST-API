using Domain.Common.Wrappers;
using Domain.DomainEvents.Inventories;
using Domain.Entities.Inventories;
using Domain.Primitives;
using System.Collections.ObjectModel;

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

    List<string> itemsToBeAdded = new();
    List<string> itemsToBeUpdated = new();
    List<string> itemsToBeRemoved = new();

    public void AddItem(string itemId, int quantity, bool notify = false)
    {
        if (string.IsNullOrEmpty(itemId))
            throw new ArgumentException($"Invalid value for {nameof(itemId)}.");

        if (quantity <= 0)
            throw new ArgumentException("Invalid value for the item quantity.");

        if (_ownedItems.TryGetValue(itemId, out var existing))
        {
            existing.Increase(quantity);
            itemsToBeUpdated.Add(itemId);
        }
        else
        {
            _ownedItems.Add(new InventoryItem(UserId, itemId, quantity));
            itemsToBeAdded.Add(itemId);
        }

        RaiseDomainEvent(new InventoryItemAddedDomainEvent(UserId, itemId, quantity, notify));
    }

    public void AddItem(InventoryItem inventoryItem, bool notify = false)
    {
        if (_ownedItems.TryGetValue(inventoryItem.ItemId, out var existing))
        {
            existing.Increase(inventoryItem.Quantity);
            itemsToBeUpdated.Add(inventoryItem.ItemId);
        }
        else
        {
            _ownedItems.Add(new InventoryItem(UserId, inventoryItem.ItemId, inventoryItem.Quantity));
            itemsToBeAdded.Add(inventoryItem.ItemId);
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
                itemsToBeRemoved.Add(itemId);
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
            itemsToBeUpdated.Add(itemId);
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

    public enum CollectionUpdate
    {
        Add,
        Update,
        Remove
    }

    public ReadOnlyCollection<string> GetCollectionUpdates(CollectionUpdate type)
    {
        switch (type)
        {
            case CollectionUpdate.Add:
                return itemsToBeAdded.AsReadOnly();
            case CollectionUpdate.Update:
                return itemsToBeUpdated.AsReadOnly();
            default:
                return itemsToBeRemoved.AsReadOnly();
        }
    }

    public void ResetCollectionUpdates(CollectionUpdate type)
    {
        switch (type)
        {
            case CollectionUpdate.Add:
                itemsToBeAdded.Clear();
                break;
            case CollectionUpdate.Update:
                itemsToBeUpdated.Clear();
                break;
            default:
                itemsToBeRemoved.Clear();
                break;
        }
    }

    public IEnumerable<string> ItemIds => OwnedItems.Select(x => x.ItemId);

    protected override object GetId() => UserId;

    protected override bool Compare(object obj)
    {
        if (obj is not Inventory entity) return false;

        return entity.UserId == UserId;
    }
}
