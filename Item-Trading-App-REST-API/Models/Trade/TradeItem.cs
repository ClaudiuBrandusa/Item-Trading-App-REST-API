namespace Item_Trading_App_REST_API.Models.Trade;

public record TradeItem
{
    public string ItemId { get; set; }

    public int Quantity { get; set; }
}
