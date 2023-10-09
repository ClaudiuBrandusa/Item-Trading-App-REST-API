namespace Item_Trading_App_REST_API.Models.Inventory;

public record InventoryItem
{
    public string Id { get; set; }

    public int Quantity { get; set; }
}
