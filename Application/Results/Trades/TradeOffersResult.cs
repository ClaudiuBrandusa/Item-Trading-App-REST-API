namespace Application.Results.Trades;

public record TradeOffersResult
{
    public IEnumerable<string> SentTradeOfferIds { get; set; } = Array.Empty<string>();

    public IEnumerable<string> ReceivedTradeOfferIds { get; set; } = Array.Empty<string>();
}
