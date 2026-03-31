namespace Application.Models.TradeItems;

public class TradeItemDTO
{
    public required string ItemId { get; set; }

    public string? ItemName { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }
}
