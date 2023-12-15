namespace Item_Trading_App_REST_API.Resources.Queries.Trade;

public record RequestTradeOfferQuery
{
    public string TradeOfferId { get; set; }

    public string UserId { get; set; }
}
