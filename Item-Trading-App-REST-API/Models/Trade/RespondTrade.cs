namespace Item_Trading_App_REST_API.Models.Trade;

public record RespondTrade
{
    public string UserId { get; set; }

    public string TradeId { get; set; }
}
