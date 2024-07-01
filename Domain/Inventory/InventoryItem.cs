namespace Domain.Inventory;

public record InventoryItem
{
    public string Id { get; set; }

    public int Quantity { get; set; }
}
