namespace Domain.Inventory;

public class LockedItem
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }

    public OwnedItem OwnedItem { get; set; }
}
