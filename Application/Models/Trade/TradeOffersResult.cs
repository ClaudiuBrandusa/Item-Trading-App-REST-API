using Application.Models.Base;

namespace Application.Models.Trade;

public record TradeOffersResult : BaseResult
{
    public IEnumerable<string> SentTradeOfferIds { get; set; }

    public IEnumerable<string> ReceivedTradeOfferIds { get; set; }
}
