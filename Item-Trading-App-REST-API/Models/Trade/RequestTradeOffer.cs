namespace Item_Trading_App_REST_API.Models.Trade;

public record RequestTradeOffer
{
    public string TradeOfferId { get; set; }

    public string UserId { get; set; }
}
