namespace Application.Models.Inventory;

public record InventoryItemQuantityNotification
{
    public bool AddAmount { get; set; }

    public int Amount { get; set; }
}
