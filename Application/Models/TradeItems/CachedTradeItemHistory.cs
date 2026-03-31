namespace Application.Models.TradeItems;

public class CachedTradeItemHistory
{
    public required string TradeId { get; set; }

    public required string ItemId { get; set; }

    public required string ItemName { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }
}
