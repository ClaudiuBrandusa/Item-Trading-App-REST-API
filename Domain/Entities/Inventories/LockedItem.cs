using Domain.Aggregates.Inventories;

namespace Domain.Entities.Inventories;

public class LockedItem : Entity
{
    public string UserId { get; private set; }

    public string ItemId { get; private set; }

    public int Quantity { get; private set; }

    public OwnedItem OwnedItem { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private LockedItem() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public LockedItem(string userId, string itemId, int quantity)
    {
        UserId = userId;
        ItemId = itemId;
        Quantity = quantity;
    }
    
    public void ChangeLockedAmount(int lockedAmount) => Quantity = lockedAmount;

    public void AddLockedAmount(int lockedAmountToAdd) => Quantity += lockedAmountToAdd;

    protected override bool Compare(object obj)
    {
        if (obj is not LockedItem entity) return false;

        return entity.UserId == UserId &&
               entity.ItemId == ItemId;
    }

    protected override object GetId() => ItemId;
}
