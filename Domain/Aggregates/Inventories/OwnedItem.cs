using Domain.Entities.Identity;
using Domain.Entities.Inventories;
using Domain.Entities.Items;

namespace Domain.Aggregates.Inventories;

public class OwnedItem : AggregateRoot
{
    public string ItemId { get; private set; }

    public string UserId { get; private set; }

    public int Quantity { get; private set; }

    public User User { get; private set; }

    public Item Item { get; private set; }

    public LockedItem LockedItem { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private OwnedItem() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public OwnedItem(string itemId, string userId, int quantity)
    {
        ItemId = itemId;
        UserId = userId;
        Quantity = quantity;
    }

    public void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
    }

    protected override bool Compare(object obj)
    {
        var entity = obj as OwnedItem;

        if (entity is null) return false;

        return entity.UserId == UserId &&
               entity.ItemId == ItemId &&
               entity.Quantity == Quantity;
    }

    protected override object GetId() => $"{UserId}{ItemId}";
}
