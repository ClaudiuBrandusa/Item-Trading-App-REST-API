using Domain.Primitives;

namespace Domain.Entities.Inventories;

public class InventoryItem : Entity
{
    public string UserId { get; private init; }

    public string ItemId { get; private init; }

    public int Quantity { get; private set; }

    public int LockedAmount { get; private set; }

    public int FreeAmount => Quantity - LockedAmount;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private InventoryItem() { } // EF Core
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public InventoryItem(string userId, string itemId, int quantity, int lockedAmount = 0)
    {
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException(nameof(itemId));
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        UserId = userId;
        ItemId = itemId;
        Quantity = quantity;
        LockedAmount = lockedAmount;
    }

    public void Increase(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Quantity += amount;
    }

    public void Drop(int amount)
    {
        if (amount <= 0 || amount > Quantity) throw new ArgumentOutOfRangeException(nameof(amount));
        Quantity -= amount;
    }

    public void Lock(int amount)
    {
        bool notEnough = Quantity < amount;
        bool invalidAmount = amount < 1;
        bool lockedAmountWouldOverflow = amount + LockedAmount > Quantity;

        if (notEnough || invalidAmount || lockedAmountWouldOverflow) throw new ArgumentOutOfRangeException(nameof(LockedAmount));
        LockedAmount += amount;
    }

    public void Unlock(int amount)
    {
        bool tooMuch = LockedAmount < amount;
        bool invalidAmount = amount < 1;

        if (tooMuch || invalidAmount) throw new ArgumentOutOfRangeException(nameof(amount));
        LockedAmount -= amount;
    }

    protected override bool Compare(object obj)
    {
        if (obj is not InventoryItem entity) return false;

        return entity.ItemId == ItemId;
    }

    protected override object GetId() => ItemId;
}
