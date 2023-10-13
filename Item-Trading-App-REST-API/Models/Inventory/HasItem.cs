namespace Item_Trading_App_REST_API.Models.Inventory;

public record HasItem
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }
}
