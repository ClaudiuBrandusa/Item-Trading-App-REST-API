namespace Item_Trading_App_REST_API.Models.Inventory;

public record InventoryItemQuantityNotification
{
    public bool AddAmount { get; set; }

    public int Amount { get; set; }
}
