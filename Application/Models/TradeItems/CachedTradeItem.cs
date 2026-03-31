namespace Application.Models.TradeItems;

public class CachedTradeItem
{
    public required string TradeId { get; set; }

    public required string ItemId { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }
}
