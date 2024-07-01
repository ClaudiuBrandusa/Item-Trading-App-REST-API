namespace Domain.TradeItems;

public record TradeItem
{
    public string ItemId { get; set; }

    public string Name { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }
}
