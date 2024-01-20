namespace Item_Trading_App_REST_API.Resources.Queries.Trade;

public record ListTradesQuery
{
    public string UserId { get; set; }

    public string[] TradeItemIds { get; set; } = new string[0];
}
