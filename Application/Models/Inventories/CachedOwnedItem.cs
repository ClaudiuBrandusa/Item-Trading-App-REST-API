namespace Application.Models.Inventories;

public class CachedOwnedItem
{
    public required string ItemId { get; set; }

    public required string UserId { get; set; }

    public int Quantity { get; set; }
}
